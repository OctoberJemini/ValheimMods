using System;
using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
namespace OctoberJemini.SafeItemStacks {
 [BepInPlugin(Guid,"Safe Item Stacks","1.0.0")]
 [BepInDependency("net.mtnewton.itemstacks",BepInDependency.DependencyFlags.SoftDependency)]
 public sealed class Plugin : BaseUnityPlugin {
  public const string Guid="OctoberJemini.SafeItemStacks";
  private static ConfigEntry<int> size;
  private static ConfigEntry<int> sealSize;
  private static ConfigEntry<int> meadSize;
  private static ConfigEntry<int> utilitySize;
  private Harmony harmony;
  private void Awake(){
   size=Config.Bind("Stacking","StackSize",50,new ConfigDescription("Maximum dragon eggs per stack. Applies only to dragon eggs.",new AcceptableValueRange<int>(2,1000)));
   sealSize=Config.Bind("Stacking","SealBreakerStackSize",50,new ConfigDescription("Stack limit for completed seal breakers only. Does not reduce a higher existing limit. Restart after changing.",new AcceptableValueRange<int>(2,1000)));
   meadSize=Config.Bind("Stacking","MeadBaseStackSize",50,new ConfigDescription("Stack limit for mead bases. Fermenters still consume one base per batch. Does not reduce a higher existing limit. Restart after changing.",new AcceptableValueRange<int>(2,1000)));
   utilitySize=Config.Bind("Stacking","UtilityStackSize",50,new ConfigDescription("Stack limit for swamp keys and saddles only. Equipment and all Dyrnwyn items are excluded. Does not reduce higher existing limits. Restart after changing.",new AcceptableValueRange<int>(2,1000)));
   harmony=new Harmony(Guid);harmony.PatchAll(typeof(Plugin).Assembly);
   var other=AccessTools.TypeByName("ItemStacks.ItemStacksPlugin");
   if(other!=null){var method=AccessTools.Method(other,"ModifyItemStackSizeAndWeight");if(method!=null)harmony.Patch(method,postfix:new HarmonyMethod(typeof(Plugin),"AfterItemStacks"));}
   Logger.LogInfo("Stack limits: dragon eggs "+size.Value+", seal breakers "+sealSize.Value+", mead bases "+meadSize.Value+", selected utility items "+utilitySize.Value+". Weight, teleport rules and fermenter consumption unchanged.");
  }
  private static void Apply(ItemDrop item){
   if(!item || item.m_itemData==null || item.m_itemData.m_shared==null)return;
   var shared=item.m_itemData.m_shared;
   // Never broaden stacking for equipment or items with per-instance wear/upgrades.
   if(shared.m_useDurability || shared.m_maxQuality>1 ||
      (shared.m_itemType!=ItemDrop.ItemData.ItemType.Material && shared.m_itemType!=ItemDrop.ItemData.ItemType.Misc))return;
   var prefab=item.m_itemData.m_dropPrefab;
   string prefabName=prefab ? prefab.name : item.gameObject.name;
   shared.m_maxStackSize=StackPolicy.Limit(prefabName,shared.m_name,shared.m_maxStackSize,size.Value,sealSize.Value,meadSize.Value,utilitySize.Value);
  }
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
