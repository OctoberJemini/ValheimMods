using System;
using BepInEx;
using BepInEx.Configuration;
using UnityEngine;
namespace OctoberJemini.InventoryFinders {
 [BepInPlugin(Guid,"Auto Wishbone & Andvaranaut","1.0.1")]
 [BepInDependency("randyknapp.mods.epicloot",BepInDependency.DependencyFlags.SoftDependency)]
 public sealed class Plugin : BaseUnityPlugin {
  public const string Guid="OctoberJemini.InventoryFinders";
  private ConfigEntry<bool> findersEnabled;
  private float nextCheck;
  private readonly FinderState[] finders={new FinderState("Wishbone"),new FinderState("Andvaranaut")};
  private sealed class FinderState {
   public readonly string Prefab;
   public Player Owner;public StatusEffect Added;public int Hash;public bool Failed;
   public FinderState(string prefab){Prefab=prefab;}
  }
  private void Awake(){
   findersEnabled=Config.Bind("General","Enabled",true,"Activate native Wishbone and Andvaranaut finders only while their items are carried in your inventory. No equipment slots required; original ranges.");
   Logger.LogInfo("Auto Wishbone & Andvaranaut loaded. Carry a Wishbone or Andvaranaut to activate its finder.");
  }
  private void Update(){
   if(Time.unscaledTime<nextCheck)return;nextCheck=Time.unscaledTime+.5f;
   foreach(var finder in finders){
    if(finder.Failed)continue;
    try{Refresh(finder);}catch(Exception ex){finder.Failed=true;Release(finder);Logger.LogError(finder.Prefab+" finder stopped after an error: "+ex);}
   }
  }
  private void Refresh(FinderState finder){
   var player=Player.m_localPlayer;
   if(finder.Owner!=player)Release(finder);
   if(!findersEnabled.Value || !player || player.IsDead()){Release(finder);return;}
   if(!ObjectDB.instance){Release(finder);return;}
   var prefab=ObjectDB.instance.GetItemPrefab(finder.Prefab);
   var item=prefab?prefab.GetComponent<ItemDrop>():null;
   // Epic Loot is optional: a missing ring does not stop the vanilla Wishbone.
   var template=item!=null && item.m_itemData!=null && item.m_itemData.m_shared!=null?item.m_itemData.m_shared.m_equipStatusEffect:null;
   if(!template){Release(finder);return;}
   // Trust the effect attached to the actual item: Epic Loot replaces the vanilla Wishbone finder.
   // Keep its exact identity, icon and filters; the ring and bone have distinct native hashes.
   bool carrying=false;var inventory=player.GetInventory();
   if(inventory!=null)foreach(var carried in inventory.GetAllItems())
    if(carried!=null && carried.m_shared!=null && carried.m_shared.m_name==item.m_itemData.m_shared.m_name){carrying=true;break;}
   if(!carrying){Release(finder);return;}
   var effects=player.GetSEMan();if(effects==null)return;
   finder.Hash=template.NameHash();var current=effects.GetStatusEffect(finder.Hash);
   if(current){ShowIcon(current,item.m_itemData);if(finder.Added && !ReferenceEquals(current,finder.Added))finder.Added=null;return;}
   finder.Owner=player;finder.Added=effects.AddStatusEffect(template,false);
   if(finder.Added)ShowIcon(finder.Added,item.m_itemData);
   if(finder.Added)Logger.LogInfo(finder.Prefab+" inventory finder active; native range and pulses preserved.");
  }
  private static void ShowIcon(StatusEffect effect,ItemDrop.ItemData item) {
   if(!effect.m_icon)effect.m_icon=item.GetIcon();
   effect.m_hidden=false;
  }
  private static bool Equipped(Player player,int hash){
   var inventory=player.GetInventory();if(inventory==null)return false;
   foreach(var item in inventory.GetEquippedItems()){
    var effect=item.m_shared.m_equipStatusEffect;
    if(effect && effect.NameHash()==hash)return true;
   }
   return false;
  }
  private void Release(FinderState finder){
   try{
    if(finder.Owner && finder.Added && !Equipped(finder.Owner,finder.Hash)){
     var effects=finder.Owner.GetSEMan();
     if(effects!=null && ReferenceEquals(effects.GetStatusEffect(finder.Hash),finder.Added))effects.RemoveStatusEffect(finder.Added,true);
    }
   }catch(Exception ex){Logger.LogWarning(finder.Prefab+" cleanup: "+ex.Message);}
   finally{finder.Owner=null;finder.Added=null;}
  }
  private void OnDisable(){foreach(var finder in finders)Release(finder);}
  private void OnDestroy(){foreach(var finder in finders)Release(finder);}
 }
}
