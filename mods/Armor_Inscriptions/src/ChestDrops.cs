using System;
using System.Reflection;
using HarmonyLib;
using UnityEngine;
namespace OctoberJemini.ArmorInscriptions {
 // Runs after the coin mod so its staged replacement cannot overwrite this addition.
 [HarmonyPatch(typeof(Container),"AddDefaultItems")]
 internal static class ChestDrops {
  const string Rolled="oct_inscription_chest_roll_v1";
  static readonly FieldInfo InventoryField=AccessTools.Field(typeof(Container),"m_inventory");
  static readonly MethodInfo Save=AccessTools.Method(typeof(Container),"Save");
  [HarmonyPostfix,HarmonyPriority(Priority.Last),HarmonyAfter("OctoberJemini.WorthwhileTreasures","randyknapp.mods.epicloot")]
  static void Postfix(Container __instance,ZNetView ___m_nview) {
   try {
    var view=___m_nview;
    if(!view||!view.IsOwner()||view.GetZDO()==null)return;
    var zdo=view.GetZDO();if(zdo.GetBool(Rolled,false))return;
    var piece=__instance.GetComponent<Piece>();if(piece&&piece.IsPlacedByPlayer())return;
    int tier=ChestIdentity.Tier(__instance,view);if(tier==0)return;
    // Record losses and pre-unlock generation too; reopening never retries a roll.
    zdo.Set(Rolled,true);
    bool unlocked=ZoneSystem.instance&&ZoneSystem.instance.GetGlobalKey("defeated_eikthyr");
    if(!ChestPolicy.PatchWins(tier,UnityEngine.Random.Range(0,100),unlocked))return;
    var inv=(Inventory)InventoryField.GetValue(__instance);
    string id=Rules.Ids[UnityEngine.Random.Range(0,Rules.Ids.Length)];
    var prefab=ObjectDB.instance?ObjectDB.instance.GetItemPrefab("OCT_Inscription_"+id):null;
    // Use the field directly: GetInventory triggers Epic Loot's deferred gear roll.
    if(inv==null||!prefab||!inv.AddItem(prefab,1)) {
     Plugin.Instance.Error(new InvalidOperationException("Treasure patch skipped: missing inventory/prefab or no room. Original loot preserved."));return;
    }
    Save.Invoke(__instance,null);
   } catch(Exception e){Plugin.Instance.Error(e);}
  }
 }
}
