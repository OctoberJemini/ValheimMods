using System;
using System.Collections.Generic;
using System.Linq;
using BepInEx.Configuration;
using Jotunn.Managers;
using UnityEngine;
namespace OctoberJemini.FishAttractor {
 public sealed class FishTrap:MonoBehaviour {
  private sealed class Catch {internal string Fish;internal float Weight;}
  private static readonly Dictionary<string,List<Catch>> Catalog=new Dictionary<string,List<Catch>>(StringComparer.Ordinal);
  internal static int BaitKinds {get{return Catalog.Count;}}
  private const string DueKey="oct_trap_due";
  private static ConfigEntry<float> interval,firstDelay;
  private static ConfigEntry<int> storage;
  private ZNetView view;private Container container;
  internal static void BuildCatalog(){
   if(!ZNetScene.instance)return;
   Catalog.Clear();
   foreach(var prefab in ZNetScene.instance.m_prefabs){
    if(!prefab)continue;var fish=prefab.GetComponent<Fish>();if(!fish||fish.m_baits==null)continue;
    var item=prefab.GetComponent<ItemDrop>()?prefab:fish.m_pickupItem;
    if(!item||!item.GetComponent<ItemDrop>())continue;
    foreach(var bait in fish.m_baits){
     if(!bait.m_bait||bait.m_chance<=0)continue;string key=bait.m_bait.gameObject.name;
     List<Catch> list;if(!Catalog.TryGetValue(key,out list)){list=new List<Catch>();Catalog.Add(key,list);}
     if(!list.Any(c=>c.Fish==item.name))list.Add(new Catch{Fish=item.name,Weight=bait.m_chance});
    }
   }
  }
  private void Awake(){
   view=GetComponent<ZNetView>();container=GetComponent<Container>();
   if(!view||!view.IsValid()||!container)return;
   var config=BepInEx.Bootstrap.Chainloader.PluginInfos[Plugin.Guid].Instance.Config;
   interval=config.Bind("Fish trap","Seconds between catches",300f,new ConfigDescription("World-time seconds per successful catch. No accumulated catch-up burst.",new AcceptableValueRange<float>(30,3600)));
   firstDelay=config.Bind("Fish trap","First catch delay",60f,new ConfigDescription("Delay after first loading fishing bait.",new AcceptableValueRange<float>(15,600)));
   storage=config.Bind("Fish trap","Stored fish limit",20,new ConfigDescription("Maximum number of caught fish in the trap, across species.",new AcceptableValueRange<int>(1,100)));
   InvokeRepeating("Tick",UnityEngine.Random.Range(4f,9f),15f);
  }
  private void Tick(){
   if(!view||!view.IsValid()||!view.IsOwner()||!ZNet.instance||!ZoneSystem.instance||!container)return;
   try{
    if(Catalog.Count==0)BuildCatalog();
    if(container.IsInUse())return;
    var inventory=container.GetInventory();if(inventory==null)return;
    var items=inventory.GetAllItems();
    int count=items.Where(i=>i.m_dropPrefab&&Catalog.Values.Any(cs=>cs.Any(c=>c.Fish==i.m_dropPrefab.name))).Sum(i=>i.m_stack);
    var bait=items.FirstOrDefault(i=>i.m_dropPrefab&&Catalog.ContainsKey(i.m_dropPrefab.name));
    if(bait==null||count>=storage.Value)return;
    var zdo=view.GetZDO();long now=ZNet.instance.GetTime().Ticks,due=zdo.GetLong(DueKey,0);
    if(due==0){zdo.Set(DueKey,ZNet.instance.GetTime().AddSeconds(firstDelay.Value).Ticks);return;}
    if(now<due)return;
    Vector3 point;if(!Water(out point))return;
    var choices=Catalog[bait.m_dropPrefab.name];
    // Exclude fish that cannot fit. A full trap never consumes bait.
    var fits=choices.Where(c=>{var p=PrefabManager.Instance.GetPrefab(c.Fish);return p&&inventory.CanAddItem(p,1);}).ToList();
    if(fits.Count==0)return;
    int index=TrapRules.Choose(UnityEngine.Random.value,fits.Select(c=>c.Weight).ToArray());if(index<0)return;
    var selected=PrefabManager.Instance.GetPrefab(fits[index].Fish);if(!selected)return;
    if(!TrapRules.CanCatch(count,storage.Value,false,true,true,now,due))return;
    // Retain an exact refund before removing bait. The native container persists inventory changes.
    var refund=bait.Clone();refund.m_stack=1;
    if(!inventory.RemoveItem(bait,1))return;
    if(!inventory.AddItem(selected,1)){
     if(!inventory.AddItem(refund))ItemDrop.DropItem(refund,1,transform.position+Vector3.up,Quaternion.identity);
     return;
    }
    zdo.Set(DueKey,ZNet.instance.GetTime().AddSeconds(interval.Value).Ticks);
   }catch(Exception e){Debug.LogWarning("[Fish Trap] Catching paused: "+e.Message);CancelInvoke("Tick");}
  }
  private bool Water(out Vector3 result){
   // Fixed nearby probes, not a scan of spawned fish or world locations.
   for(int i=0;i<9;i++){
    float angle=i*Mathf.PI/4,radius=i==0?0:3;
    var p=transform.position+new Vector3(Mathf.Cos(angle)*radius,0,Mathf.Sin(angle)*radius);
    float water=Floating.GetLiquidLevel(new Vector3(p.x,transform.position.y-4,p.z),0,LiquidType.Water);
    if(BaitPolicy.WaterSuitable(water,ZoneSystem.instance.GetGroundHeight(p),Plugin.Depth.Value,transform.position.y)){result=p;return true;}
   }
   result=Vector3.zero;return false;
  }
 }
}
