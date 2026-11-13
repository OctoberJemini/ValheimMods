using System;
using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
namespace OctoberJemini.DragonEggStacks {
 [BepInPlugin(Guid,"Dragon Egg Stacks","1.0.0")]
 [BepInDependency("net.mtnewton.itemstacks",BepInDependency.DependencyFlags.SoftDependency)]
 public sealed class Plugin : BaseUnityPlugin {
  public const string Guid="OctoberJemini.DragonEggStacks";
  private static ConfigEntry<int> size;
  private Harmony harmony;
  private void Awake(){
   size=Config.Bind("Stacking","StackSize",50,new ConfigDescription("Maximum dragon eggs per stack. Applies only to dragon eggs.",new AcceptableValueRange<int>(2,1000)));
   harmony=new Harmony(Guid);harmony.PatchAll(typeof(Plugin).Assembly);
   var other=AccessTools.TypeByName("ItemStacks.ItemStacksPlugin");
   if(other!=null){var method=AccessTools.Method(other,"ModifyItemStackSizeAndWeight");if(method!=null)harmony.Patch(method,postfix:new HarmonyMethod(typeof(Plugin),"AfterItemStacks"));}
   Logger.LogInfo("Dragon eggs stack to "+size.Value+"; weight and teleport rules unchanged.");
  }
  private static void Apply(ItemDrop item){if(item && item.m_itemData!=null && item.m_itemData.m_shared!=null && item.m_itemData.m_shared.m_name=="$item_dragonegg")item.m_itemData.m_shared.m_maxStackSize=size.Value;}
  private static void ApplyDB(ObjectDB db){if(db && db.m_items!=null)foreach(var go in db.m_items)if(go)Apply(go.GetComponent<ItemDrop>());}
  private static void AfterItemStacks(ObjectDB __0){ApplyDB(__0);}
  [HarmonyPatch(typeof(ObjectDB),"Awake")]
  private static class DBAwake {
   [HarmonyPostfix,HarmonyPriority(Priority.Last),HarmonyAfter("net.mtnewton.itemstacks")]
   private static void Postfix(ObjectDB __instance){ApplyDB(__instance);}
  }
  [HarmonyPatch(typeof(ObjectDB),"CopyOtherDB")]
  private static class DBCopy {
   [HarmonyPostfix,HarmonyPriority(Priority.Last),HarmonyAfter("net.mtnewton.itemstacks")]
   private static void Postfix(ObjectDB __instance){ApplyDB(__instance);}
  }
  [HarmonyPatch(typeof(ItemDrop),"Awake")]
  private static class DropAwake {
   [HarmonyPostfix,HarmonyPriority(Priority.Last)]
   private static void Postfix(ItemDrop __instance){Apply(__instance);}
  }
  private void OnDestroy(){if(harmony!=null)harmony.UnpatchSelf();}
 }
}
