using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using BepInEx;
using EpicLoot;
using HarmonyLib;
using Jotunn.Utils;
using Newtonsoft.Json;
using UnityEngine;

[assembly: AssemblyVersion("1.0.0.0")]
namespace OctoberJemini.AndvaranautEnchanting {
 [BepInPlugin(Guid,"Andvaranaut Enchanting","1.0.0")]
 [BepInDependency("randyknapp.mods.epicloot")]
 [BepInDependency("com.jotunn.jotunn","2.30.2")]
 [NetworkCompatibility(CompatibilityLevel.EveryoneMustHaveMod,VersionStrictness.Patch)]
 public sealed class Plugin:BaseUnityPlugin {
  public const string Guid="OctoberJemini.AndvaranautEnchanting";
  internal const string Backup="oct_andvaranaut_original_magic_v1";
  private Harmony harmony;private float next;
  [ThreadStatic] private static bool migrating;
  private static Plugin instance;
  private void Awake(){instance=this;harmony=new Harmony(Guid);harmony.PatchAll();}
  private void OnDestroy(){if(harmony!=null)harmony.UnpatchSelf();}
  private void Update(){
   if(Time.unscaledTime<next)return;next=Time.unscaledTime+1;
   var prefab=ObjectDB.instance?ObjectDB.instance.GetItemPrefab("Andvaranaut"):null;
   if(prefab)Normalize(prefab.GetComponent<ItemDrop>().m_itemData);
   var player=Player.m_localPlayer;if(player)foreach(var item in player.GetInventory().GetAllItems())Normalize(item);
  }
  internal static bool IsRing(ItemDrop.ItemData item){
   if(item==null||item.m_shared==null)return false;
   if(item.m_dropPrefab)return item.m_dropPrefab.name=="Andvaranaut";
   var prefab=ObjectDB.instance?ObjectDB.instance.GetItemPrefab("Andvaranaut"):null;
   return prefab&&ReferenceEquals(item.m_shared,prefab.GetComponent<ItemDrop>().m_itemData.m_shared);
  }
  internal static void Normalize(ItemDrop.ItemData item){
   if(migrating||!IsRing(item))return;
   // Never strip the marker unless the real native ability is already present.
   var finder=item.m_shared.m_equipStatusEffect;
   if(!finder||finder.name!="Andvaranaut")return;
   migrating=true;
   try {
    var magic=item.GetMagicItem();if(magic==null||magic.Effects==null||!magic.Effects.Any(e=>e.EffectType=="Andvaranaut"))return;
    string raw=MagicItemData.RawOf(item);
    var copy=JsonConvert.DeserializeObject<MagicItem>(JsonConvert.SerializeObject(magic));
    if(copy==null)throw new InvalidOperationException("Cannot copy ring enchantments");
    // Native removal also remaps augmented and tempered indices. Preserve all other rolls.
    foreach(int i in MarkerPolicy.MarkerIndices(copy.Effects.Select(e=>e.EffectType).ToArray()))copy.RemoveEffectAt(i);
    if(item.m_customData==null)item.m_customData=new Dictionary<string,string>();
    if(!item.m_customData.ContainsKey(Backup))item.m_customData.Add(Backup,raw??"");
    bool originalOnly=MarkerPolicy.EmptyOriginal(copy.Effects.Count,copy.SocketCount,copy.Sockets==null?0:copy.Sockets.Count,copy.LegendaryID,copy.SetID,copy.IsUnidentified,copy.DisplayName);
    if(originalOnly)item.RemoveMagicItem();else item.SaveMagicItem(copy);
    // No edits to SharedData, finder, quality, durability, stack, slot or crafter.
   }catch(Exception e){if(instance)instance.Logger.LogError("Andvaranaut migration stopped: "+e);}
   finally{migrating=false;}
  }
  [HarmonyPatch(typeof(EpicLoot.EpicLoot),"SetupAndvaranaut")]
  private static class Setup {
   private static void Postfix(){var prefab=EpicAssets.AssetBundle.LoadAsset<GameObject>("Andvaranaut");if(prefab)Normalize(prefab.GetComponent<ItemDrop>().m_itemData);}
  }
  [HarmonyPatch]
  private static class MagicQueries {
   private static IEnumerable<MethodBase> TargetMethods(){return typeof(ItemDataExtensions).GetMethods(BindingFlags.Public|BindingFlags.Static).Where(m=>(m.Name=="IsMagic"||m.Name=="CanBeDisenchanted"||m.Name=="CanBeAugmented"||m.Name=="CanBeRunified")&&m.GetParameters().Length>0&&m.GetParameters()[0].ParameterType==typeof(ItemDrop.ItemData)).Cast<MethodBase>();}
   private static void Prefix(ItemDrop.ItemData __0){Normalize(__0);}
  }
  // Permit only this real ring through item-category/restricted-item checks; leave all
  // resource costs, station upgrades, rarity availability and effect-roll rules native.
  [HarmonyPatch(typeof(EpicLoot.EpicLoot),"IsNotRestrictedItem")]
  private static class Restriction {
   private static void Postfix(ItemDrop.ItemData __0,ref bool __result){if(IsRing(__0))__result=true;}
  }
  [HarmonyPatch(typeof(EpicLoot.EpicLoot),"IsAllowedMagicItemType")]
  private static class Category {
   private static void Postfix(ItemDrop.ItemData __0,ref bool __result){if(IsRing(__0))__result=true;}
  }
  [HarmonyPatch]
  private static class Tooltip {
   private static MethodBase TargetMethod(){return typeof(ItemDrop.ItemData).GetMethods(BindingFlags.Public|BindingFlags.Static).Single(m=>m.Name=="GetTooltip"&&m.GetParameters()[0].ParameterType==typeof(ItemDrop.ItemData));}
   private static void Postfix(ItemDrop.ItemData __0,ref string __result){if(IsRing(__0))__result+="\n\n<color=#E5C36B>Unique ability: Treasure finder</color>\nPermanent. Enchanting and disenchanting do not remove it.";}
  }
 }
}
