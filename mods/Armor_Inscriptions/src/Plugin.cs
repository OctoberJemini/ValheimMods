using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using BepInEx;
using HarmonyLib;
using Jotunn.Entities;
using Jotunn.Managers;
using Jotunn.Utils;
using UnityEngine;
[assembly: AssemblyVersion("0.5.2.0")]
namespace OctoberJemini.ArmorInscriptions {
 [BepInPlugin(Guid,"Armor Inscriptions","0.5.2")]
 [BepInDependency("com.jotunn.jotunn","2.30.2")]
 [BepInIncompatibility("Basil_NoStamCosts")]
 [BepInIncompatibility("org.tonsit.interactwhilebuilding")]
 [NetworkCompatibility(CompatibilityLevel.EveryoneMustHaveMod,VersionStrictness.Patch)]
 public sealed class Plugin:BaseUnityPlugin {
  public const string Guid="OctoberJemini.ArmorInscriptions";
  internal static Plugin Instance;
  internal static readonly Dictionary<string,CustomItem> Items=new Dictionary<string,CustomItem>();
  private Harmony harmony;
  private void Awake(){
   Instance=this;PrefabManager.OnVanillaPrefabsAvailable+=Register;
   var overrides=Config.Bind("Armor tiers","Overrides","","Optional comma-separated armor prefab=patch count overrides (1 to 7). Unknown armor is blocked until mapped.");
   foreach(var entry in overrides.Value.Split(new[]{','},StringSplitOptions.RemoveEmptyEntries)){var pair=entry.Split('=');int cost;if(pair.Length!=2||!int.TryParse(pair[1],out cost)||!ArmorTiers.Override(pair[0],cost))Logger.LogWarning("Invalid armor tier override: "+entry);}
   harmony=new Harmony(Guid);
   try{harmony.PatchAll();}catch{harmony.UnpatchSelf();throw;}
   Logger.LogInfo("Armor Inscriptions 0.5.2: private test build. Six item-bound inscriptions; Hildir purchases and rare treasure finds.");
  }
  private void Update(){ApplyMenu.Tick();}
  private void OnDestroy(){ApplyMenu.Close();PrefabManager.OnVanillaPrefabsAvailable-=Register;if(harmony!=null)harmony.UnpatchSelf();}
  internal static string Title(string id){return id=="snorkel"?"Snorkel Inscription":id=="tidewalker"?"Tidewalker Inscription":char.ToUpperInvariant(id[0])+id.Substring(1)+"'s Inscription";}
  internal static string Benefit(string id){
   switch(id){case "runner":return "Running costs no stamina.";case "leaper":return "Jumping costs no stamina.";
    case "snorkel":return "Swimming costs no stamina and prevents exhaustion drowning.";
    case "tidewalker":return "Swim 25% faster.";
    case "builder":return "Hammer building, removal and repairs cost no stamina.";
    default:return "Hoe and cultivator use cost no stamina.";}
  }
  internal static string Slot(string id){return Rules.IsHelmet(id)?"helmet":Rules.IsLegs(id)?"leg armor":"body armor";}
  internal static bool Compatible(ItemDrop.ItemData item,string id){return Rules.IsKnown(id)&&item!=null&&item.m_shared!=null&&item.m_shared.m_itemType==(Rules.IsHelmet(id)?ItemDrop.ItemData.ItemType.Helmet:Rules.IsLegs(id)?ItemDrop.ItemData.ItemType.Legs:ItemDrop.ItemData.ItemType.Chest);}
  internal static string PatchId(ItemDrop.ItemData item){
   if(item==null||!item.m_dropPrefab)return null;
   foreach(string id in Rules.Ids)if(item.m_dropPrefab.name=="OCT_Inscription_"+id)return id;
   return null;
  }
  private void Register(){
   foreach(string id in Rules.Ids){
    if(Items.ContainsKey(id))continue;
    try{
     var item=new CustomItem("OCT_Inscription_"+id,"LeatherScraps");
     var shared=item.ItemDrop.m_itemData.m_shared;
     shared.m_name=Title(id);shared.m_description="Apply to "+Slot(id)+". "+Benefit(id)+"\n\nUse an Inscription Bench to choose armor. Builder and Cultivator need one matching patch; other inscriptions need 1-7 by armor tier. Bench extensions still follow armor tier. Consumed on application. Permanent on that item; works only while equipped. Replacing armor requires another patch.";
     shared.m_itemType=ItemDrop.ItemData.ItemType.Material;shared.m_weight=0.1f;
     shared.m_maxStackSize=20;shared.m_maxQuality=1;shared.m_value=0;shared.m_questItem=false;
     shared.m_icons=new[]{LoadIcon(id)};
     if(!ItemManager.Instance.AddItem(item))throw new InvalidOperationException("Registration rejected");
     Items.Add(id,item);
    }catch(Exception e){Logger.LogError("Cannot register "+id+": "+e);}
   }
   try{Bench.Register();TailoringExtensions.Register();}catch(Exception e){Logger.LogError("Inscription Bench registration failed: "+e);}
  }
  internal static Sprite LoadIcon(string id){
   using(var stream=Assembly.GetExecutingAssembly().GetManifestResourceStream("patch."+id+".png")){
    if(stream==null)throw new FileNotFoundException("Missing patch icon: "+id);
    byte[] bytes=new byte[stream.Length];int total=0;while(total<bytes.Length){int n=stream.Read(bytes,total,bytes.Length-total);if(n==0)throw new EndOfStreamException();total+=n;}
    var texture=new Texture2D(2,2,TextureFormat.RGBA32,false);
    if(!ImageConversion.LoadImage(texture,bytes))throw new InvalidOperationException("Invalid patch PNG");
    texture.filterMode=FilterMode.Bilinear;texture.wrapMode=TextureWrapMode.Clamp;
    return Sprite.Create(texture,new Rect(0,0,texture.width,texture.height),new Vector2(.5f,.5f),100);
   }
  }
  internal void Error(Exception e){Logger.LogError(e);}
  [HarmonyPatch(typeof(Trader),"GetAvailableItems")]
  private static class TraderItems{
   private static void Postfix(Trader __instance,ref List<Trader.TradeItem> __result){
    if(__instance.name.Replace("(Clone)","")!="Hildir"||!ZoneSystem.instance)return;
    int price=Rules.Price(key=>ZoneSystem.instance.GetGlobalKey(key));
    if(price==0)return;
    foreach(var pair in Items){
     if(__result.Any(x=>x.m_prefab==pair.Value.ItemDrop))continue;
     __result.Add(new Trader.TradeItem{m_prefab=pair.Value.ItemDrop,m_stack=1,m_price=price,m_requiredGlobalKey=Rules.BossKeys[0],m_tooltip="",m_name="",m_buyKey="",m_incrementKey="",m_buyPlayerEffects=new EffectList()});
    }
   }
  }
  [HarmonyPatch(typeof(Humanoid),"UseItem")]
  private static class Use{
   private static bool Prefix(Humanoid __instance,Inventory __0,ItemDrop.ItemData __1){
    string id=PatchId(__1);if(id==null)return true;
    var p=__instance as Player;
    if(p!=Player.m_localPlayer||!p)return false;
    var inventory=__0??p.GetInventory();
    if(inventory!=p.GetInventory()||!inventory.ContainsItem(__1)){p.Message(MessageHud.MessageType.Center,"Move the patch into your inventory first.");return false;}
    p.Message(MessageHud.MessageType.Center,"Use an Inscription Bench to apply this patch.");return false;
   }
  }
  [HarmonyPatch]
  private static class Tooltip{
   private static MethodBase TargetMethod(){return typeof(ItemDrop.ItemData).GetMethods(BindingFlags.Public|BindingFlags.Static).Single(m=>m.Name=="GetTooltip"&&m.GetParameters()[0].ParameterType==typeof(ItemDrop.ItemData));}
   [HarmonyPriority(Priority.Last)]
   private static void Postfix(ItemDrop.ItemData __0,ref string __result){
    if(__0==null)return;
    var active=Rules.Ids.Where(id=>Compatible(__0,id)&&Rules.Has(__0.m_customData,id)).ToArray();
    if(active.Length==0)return;
    __result+="\n\n<color=#E5C36B>Inscriptions</color>";
    foreach(string id in active)__result+="\n<color=#E5C36B>"+Title(id)+"</color> — "+Benefit(id);
    __result+="\nActive while equipped. Permanent on this item.";
   }
  }
 }
}
