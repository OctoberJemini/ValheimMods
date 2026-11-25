using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HarmonyLib;
namespace OctoberJemini.ArmorInscriptions {
 // Vanilla upgrades remove and recreate equipment. Carry only our keys to the
 // replacement in the same inventory cell; never overwrite Epic Loot's metadata.
 internal static class UpgradePreservation {
  internal sealed class Context {
   internal ItemDrop.ItemData Source;
   internal Inventory Inventory;
   internal Vector2i Position;
   internal string Prefab;
   internal Dictionary<string,string> Keys;
   internal Context Parent;
   internal bool Applied;
  }
  [ThreadStatic] private static Context current;
  [HarmonyPatch(typeof(InventoryGui),"DoCrafting")]
  private static class Craft {
   private static void Prefix(Player __0,ItemDrop.ItemData ___m_craftUpgradeItem,out Context __state){
    __state=new Context{Parent=current};current=__state;
    var source=___m_craftUpgradeItem;
    if(source==null||!source.m_dropPrefab||!__0||!__0.GetInventory().ContainsItem(source))return;
    var keys=new Dictionary<string,string>();
    foreach(string id in Rules.Ids)if(Plugin.Compatible(source,id)&&Rules.Has(source.m_customData,id))keys.Add(Rules.Key(id),"1");
    if(keys.Count==0)return;
    __state.Source=source;__state.Inventory=__0.GetInventory();__state.Position=source.m_gridPos;
    __state.Prefab=source.m_dropPrefab.name;__state.Keys=keys;
   }
   private static void Finalizer(Context __state){if(__state!=null)current=__state.Parent;}
  }
  [HarmonyPatch]
  private static class Added {
   private static MethodBase TargetMethod(){
    return AccessTools.Method(typeof(Inventory),"AddItem",new[]{typeof(string),typeof(int),typeof(int),typeof(int),typeof(long),typeof(string),typeof(Vector2i),typeof(bool),typeof(bool),typeof(bool)});
   }
   [HarmonyPriority(Priority.Last)]
   private static void Postfix(Inventory __instance,ItemDrop.ItemData __result){
    var c=current;
    if(c==null||c.Source==null||c.Applied||c.Inventory!=__instance||__result==null||!__result.m_dropPrefab||__result.m_dropPrefab.name!=c.Prefab)return;
    if(__instance.ContainsItem(c.Source)||__result.m_gridPos.x!=c.Position.x||__result.m_gridPos.y!=c.Position.y)return;
    if(__result.m_customData==null)__result.m_customData=new Dictionary<string,string>();
    foreach(var pair in c.Keys)__result.m_customData[pair.Key]=pair.Value;
    c.Applied=true;
   }
  }
 }
}
