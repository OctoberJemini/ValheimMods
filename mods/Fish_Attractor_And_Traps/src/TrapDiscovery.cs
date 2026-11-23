using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using UnityEngine;

namespace OctoberJemini.FishAttractor {
 internal static class TrapDiscovery {
  internal const string Key="oct_fishtrap_discovered_v1";
  internal static HashSet<string> Known(Player p){string s;return new HashSet<string>(p.m_customData.TryGetValue(Key,out s)?s.Split(new[]{','},StringSplitOptions.RemoveEmptyEntries):new string[0],StringComparer.Ordinal);}
  internal static void Learn(Player p,string fish){
   if(!p||p!=Player.m_localPlayer||!FishTrap.IsFish(fish))return;
   var known=Known(p);if(!known.Add(fish))return;
   p.m_customData[Key]=String.Join(",",known.OrderBy(x=>x));
   p.Message(MessageHud.MessageType.TopLeft,"Fish trap species unlocked: "+FishTrap.FishName(fish));
   TrapService.Instance.PublishProfile(true);
  }
  [HarmonyPatch(typeof(FishingFloat),"Catch")]
  private static class RodCatch {
   private static void Prefix(Fish __0,Character __1,out string __state){__state=__0&&__1==Player.m_localPlayer?FishTrap.ItemName(__0):null;}
   private static void Postfix(string __state,string __result){if(!String.IsNullOrEmpty(__state)&&!String.IsNullOrEmpty(__result))Learn(Player.m_localPlayer,__state);}
  }
  // Watch actual inventory transfers out of a trap, not possession/buying/trading fish.
  [HarmonyPatch]
  private static class Collected {
   private sealed class Transfer {internal Dictionary<string,int> Before,Credits;}
   [ThreadStatic] private static bool tracking;
   private static IEnumerable<MethodBase> TargetMethods(){return typeof(Inventory).GetMethods(BindingFlags.Public|BindingFlags.Instance).Where(m=>(m.Name=="MoveItemToThis"||m.Name=="MoveAll"||m.Name=="StackAll")&&m.GetParameters().Length>0&&m.GetParameters()[0].ParameterType==typeof(Inventory)).Cast<MethodBase>();}
   private static void Prefix(Inventory __instance,Inventory __0,out Transfer __state){
    __state=null;var p=Player.m_localPlayer;
    if(tracking||!p||p.GetInventory()!=__instance)return;
    var source=FishTrap.Loaded.FirstOrDefault(t=>t.Inventory==__0);if(!source||!source.View.IsOwner())return;
    __state=new Transfer{Before=Counts(__instance),Credits=source.Credits()};tracking=true;
   }
   private static void Postfix(Inventory __instance,Transfer __state){
    if(__state==null)return;foreach(var pair in Counts(__instance)){int before,credit;__state.Before.TryGetValue(pair.Key,out before);__state.Credits.TryGetValue(pair.Key,out credit);if(TrapEconomy.UnlockOnCollection(before,pair.Value,credit))Learn(Player.m_localPlayer,pair.Key);}
   }
   private static void Finalizer(Transfer __state){if(__state!=null)tracking=false;}
   private static Dictionary<string,int> Counts(Inventory inv){return inv.GetAllItems().Where(i=>i.m_dropPrefab&&FishTrap.IsFish(i.m_dropPrefab.name)).GroupBy(i=>i.m_dropPrefab.name).ToDictionary(g=>g.Key,g=>g.Sum(i=>i.m_stack));}
  }
  [HarmonyPatch(typeof(Container),"GetHoverText")]
  private static class Hover {
   private static void Postfix(Container __instance,ref string __result){var trap=__instance.GetComponent<FishTrap>();if(trap)__result+="\n"+trap.Hover();}
  }
 }
}
