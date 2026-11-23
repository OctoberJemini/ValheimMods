using System;
using System.Collections.Generic;
using System.Linq;
using BepInEx.Configuration;
using Jotunn.Managers;
using UnityEngine;
namespace OctoberJemini.FishAttractor {
 public sealed class FishTrap:MonoBehaviour {
  internal sealed class Catch {internal string Fish;internal float Weight;}
  private static readonly Dictionary<string,List<Catch>> Catalog=new Dictionary<string,List<Catch>>(StringComparer.Ordinal);
  private static readonly Dictionary<string,Heightmap.Biome> Habitats=new Dictionary<string,Heightmap.Biome>(StringComparer.Ordinal);
  internal static readonly List<FishTrap> Loaded=new List<FishTrap>();
  internal static int BaitKinds {get{return Catalog.Count;}}
  private static ConfigEntry<int> storage;
  private ZNetView view;private Container container;private float lastReply;
  private Inventory watched;private Dictionary<string,int> previous=new Dictionary<string,int>();
  internal ZNetView View {get{return view;}}
  internal Inventory Inventory {get{return container?container.GetInventory():null;}}
  internal static string ItemName(Fish fish){var item=fish.GetComponent<ItemDrop>();return item?fish.gameObject.name.Replace("(Clone)",""):fish.m_pickupItem?fish.m_pickupItem.name:"";}
  internal static bool IsFish(string name){if(Catalog.Count==0)BuildCatalog();return Catalog.Values.Any(cs=>cs.Any(c=>c.Fish==name));}
  internal static string FishName(string id){var go=PrefabManager.Instance.GetPrefab(id);var item=go?go.GetComponent<ItemDrop>():null;return item&&Localization.instance!=null?Localization.instance.Localize(item.m_itemData.m_shared.m_name):id;}
  internal static void BuildCatalog(){
   if(!ZNetScene.instance)return;Catalog.Clear();Habitats.Clear();
   foreach(var prefab in ZNetScene.instance.m_prefabs){
    if(!prefab)continue;var fish=prefab.GetComponent<Fish>();if(!fish||fish.m_baits==null)continue;
    string name=ItemName(fish);if(String.IsNullOrEmpty(name))continue;
    foreach(var bait in fish.m_baits){
     if(!bait.m_bait||bait.m_chance<=0)continue;string key=bait.m_bait.gameObject.name;
     List<Catch> list;if(!Catalog.TryGetValue(key,out list)){list=new List<Catch>();Catalog.Add(key,list);}
     if(!list.Any(c=>c.Fish==name))list.Add(new Catch{Fish=name,Weight=bait.m_chance});
    }
   }
   foreach(var list in Resources.FindObjectsOfTypeAll<SpawnSystemList>())foreach(var spawn in list.m_spawners){
    if(!spawn.m_enabled||spawn.m_devDisabled||!spawn.m_prefab)continue;var fish=spawn.m_prefab.GetComponent<Fish>();if(!fish)continue;
    string name=ItemName(fish);Heightmap.Biome old;Habitats.TryGetValue(name,out old);Habitats[name]=old|spawn.m_biome;
   }
   Habitats["Fish4_cave"]=Heightmap.Biome.Mountain;
  }
  internal static IEnumerable<Catch> Eligible(string bait,Heightmap.Biome biome,HashSet<string> known){
   if(Catalog.Count==0)BuildCatalog();List<Catch> list;if(!Catalog.TryGetValue(bait,out list))return new Catch[0];
   return list.Where(c=>{Heightmap.Biome native;Habitats.TryGetValue(c.Fish,out native);return TrapEconomy.Eligible(true,known.Contains(c.Fish),(int)native,(int)biome);});
  }
  private void Awake(){
   view=GetComponent<ZNetView>();container=GetComponent<Container>();if(!view||!view.IsValid()||!container)return;
   storage=BepInEx.Bootstrap.Chainloader.PluginInfos[Plugin.Guid].Instance.Config.Bind("Fish trap","Stored fish limit",20,new ConfigDescription("Maximum fish across species.",new AcceptableValueRange<int>(1,100)));
   Loaded.Add(this);lastReply=Time.unscaledTime;Watch();
   InvokeRepeating("Tick",UnityEngine.Random.Range(4f,9f),15f);
  }
  private void OnDestroy(){Loaded.Remove(this);if(watched!=null)watched.m_onChanged-=InventoryChanged;}
  private Dictionary<string,int> Counts(){return Inventory.GetAllItems().Where(i=>i.m_dropPrefab&&IsFish(i.m_dropPrefab.name)).GroupBy(i=>i.m_dropPrefab.name).ToDictionary(g=>g.Key,g=>g.Sum(i=>i.m_stack));}
  private void Watch(){if(Inventory==null||watched==Inventory)return;if(watched!=null)watched.m_onChanged-=InventoryChanged;watched=Inventory;previous=Counts();watched.m_onChanged+=InventoryChanged;}
  private void InventoryChanged(){
   var current=Counts();if(view&&view.IsValid()&&view.IsOwner())foreach(var pair in previous){int count;current.TryGetValue(pair.Key,out count);if(count<pair.Value){string key="oct_trap_credit_"+pair.Key;var zdo=view.GetZDO();zdo.Set(key,Math.Max(0,zdo.GetInt(key,0)-(pair.Value-count)));}}
   previous=current;
  }
  internal Dictionary<string,int> Credits(){return Counts().ToDictionary(p=>p.Key,p=>Math.Min(p.Value,view.GetZDO().GetInt("oct_trap_credit_"+p.Key,0)));}
  private void Tick(){
   if(!view||!view.IsValid()||!view.IsOwner()||!ZNet.instance||!ZoneSystem.instance||!container||TrapService.Instance==null)return;
   try{
    if(Catalog.Count==0)BuildCatalog();Watch();var inventory=Inventory;if(inventory==null)return;var items=inventory.GetAllItems();
    int count=items.Where(i=>i.m_dropPrefab&&IsFish(i.m_dropPrefab.name)).Sum(i=>i.m_stack);
    var bait=items.FirstOrDefault(i=>i.m_dropPrefab&&Catalog.ContainsKey(i.m_dropPrefab.name));
    Vector3 point;bool water=Water(out point);if(!water)point=transform.position;
    bool ready=!container.IsInUse()&&bait!=null&&count<storage.Value&&water;
    string[] fits=bait==null?new string[0]:Catalog[bait.m_dropPrefab.name].Where(c=>{var p=PrefabManager.Instance.GetPrefab(c.Fish);return p&&inventory.CanAddItem(p,1);}).Select(c=>c.Fish).ToArray();
    string reason=container.IsInUse()?"Paused while open":!water?"Needs suitable water":count>=storage.Value?"Fish storage full":bait==null?"Load fishing bait":fits.Length==0?"No inventory space":"";
    view.GetZDO().Set("oct_trap_local_status",reason);
    if(Time.unscaledTime-lastReply>45)view.GetZDO().Set("oct_trap_status","Waiting for fishing host or ledger recovery");
    TrapService.Instance.Request(this,ready,bait==null?"":bait.m_dropPrefab.name,fits,point);
   }catch(Exception e){view.GetZDO().Set("oct_trap_status","Paused: "+e.Message);Debug.LogWarning("[Fish Trap] "+e.Message);}
  }
  internal void Receive(ZPackage p){
   lastReply=Time.unscaledTime;
   long sequence=p.ReadLong();bool pending=p.ReadBool(),lost=p.ReadBool();string fish=p.ReadString(),baitName=p.ReadString();double due=p.ReadDouble();string status=p.ReadString(),owner=p.ReadString();float skill=p.ReadSingle();int stock=p.ReadInt(),competing=p.ReadInt();
   var zdo=view.GetZDO();zdo.Set("oct_trap_status",status);zdo.Set("oct_trap_owner_name",owner);zdo.Set("oct_trap_skill",skill);zdo.Set("oct_trap_stock",stock);zdo.Set("oct_trap_nearby",competing);zdo.Set("oct_trap_due_v2",(long)(due*TimeSpan.TicksPerSecond));
   if(!pending||sequence<=zdo.GetLong("oct_trap_applied_v2",0)||container.IsInUse())return;
   Vector3 point;if(!Water(out point))return;
   var inv=Inventory;var bait=inv.GetAllItems().FirstOrDefault(i=>i.m_dropPrefab&&i.m_dropPrefab.name==baitName);if(bait==null)return;
   List<Catch> possible;if(!Catalog.TryGetValue(baitName,out possible)||!possible.Any(c=>{var candidate=PrefabManager.Instance.GetPrefab(c.Fish);return candidate&&inv.CanAddItem(candidate,1);}))return;
   int count=inv.GetAllItems().Where(i=>i.m_dropPrefab&&IsFish(i.m_dropPrefab.name)).Sum(i=>i.m_stack);if(count>=storage.Value)return;
   var prefab=lost?null:PrefabManager.Instance.GetPrefab(fish);if(!lost&&(!prefab||!inv.CanAddItem(prefab,1)))return;
   var refund=bait.Clone();refund.m_stack=1;zdo.Set("oct_trap_applied_v2",sequence);
   if(!inv.RemoveItem(bait,1)){zdo.Set("oct_trap_applied_v2",sequence-1);return;}
   if(!lost){
    if(!inv.AddItem(prefab,1)){if(!inv.AddItem(refund))ItemDrop.DropItem(refund,1,transform.position+Vector3.up,Quaternion.identity);return;}
    string credit="oct_trap_credit_"+fish;zdo.Set(credit,zdo.GetInt(credit,0)+1);
   }
   HarmonyLib.AccessTools.Method(typeof(Container),"Save").Invoke(container,null);
  }
  internal string Hover(){
   if(!view||!view.IsValid())return "";var zdo=view.GetZDO();
   string local=zdo.GetString("oct_trap_local_status","");string state=local.Length>0?local:zdo.GetString("oct_trap_status","Waiting for owner profile");
   float skill=zdo.GetFloat("oct_trap_skill",0);double now=ZNet.instance?ZNet.instance.GetTime().Ticks/(double)TimeSpan.TicksPerSecond:0;double remaining=Math.Max(0,(zdo.GetLong("oct_trap_due_v2",0)/(double)TimeSpan.TicksPerSecond)-now);
   return "Owner: "+zdo.GetString("oct_trap_owner_name","builder")+" | Fishing "+skill.ToString("0")+"\n"+state+"\nShared stock: "+zdo.GetInt("oct_trap_stock",3)+"/3 | Nearby traps (20m): "+zdo.GetInt("oct_trap_nearby",1)+"\nAttempt: "+(TrapEconomy.Interval(skill)/60).ToString("0.#")+" min | Bait loss: "+(TrapEconomy.Loss(skill)*100).ToString("0")+"%"+(remaining>0?" | Next: "+Math.Ceiling(remaining/60)+" min":"");
  }
  private bool Water(out Vector3 result){
   for(int i=0;i<9;i++){
    float angle=i*Mathf.PI/4,radius=i==0?0:3;var p=transform.position+new Vector3(Mathf.Cos(angle)*radius,0,Mathf.Sin(angle)*radius);
    float water=Floating.GetLiquidLevel(new Vector3(p.x,transform.position.y-4,p.z),0,LiquidType.Water);
    if(BaitPolicy.WaterSuitable(water,ZoneSystem.instance.GetGroundHeight(p),Plugin.Depth.Value,transform.position.y)){result=p;return true;}
   }result=Vector3.zero;return false;
  }
 }
}
