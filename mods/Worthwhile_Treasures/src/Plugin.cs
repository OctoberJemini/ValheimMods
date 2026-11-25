using System;
using System.Reflection;
using BepInEx;
using HarmonyLib;
using Jotunn.Utils;
using UnityEngine;
[assembly:AssemblyVersion("1.1.0.0")]
namespace OctoberJemini.WorthwhileTreasures {
 [BepInPlugin(Guid,"Worthwhile Treasures","1.1.0")]
 [BepInDependency("com.jotunn.jotunn","2.30.2")]
 [BepInDependency("randyknapp.mods.epicloot",BepInDependency.DependencyFlags.SoftDependency)]
 [NetworkCompatibility(CompatibilityLevel.EveryoneMustHaveMod,VersionStrictness.Patch)]
 public sealed class Plugin:BaseUnityPlugin {
  public const string Guid="OctoberJemini.WorthwhileTreasures";
  const string Applied="oct_treasure_reward_v1", Jackpot="oct_treasure_jackpot_v1", Heard="oct_treasure_heard_v1";
  BepInEx.Configuration.ConfigEntry<bool> diagnosticLogging; static Plugin instance; Harmony harmony; static AudioClip chime;
  static readonly MethodInfo Save=AccessTools.Method(typeof(Container),"Save");
  static readonly FieldInfo InventoryField=AccessTools.Field(typeof(Container),"m_inventory");
  void Awake(){instance=this;diagnosticLogging=Config.Bind("Diagnostics","VerboseLogging",false,"Log chest identities and contents for troubleshooting. Disabled by default.");harmony=new Harmony(Guid);harmony.PatchAll();Logger.LogInfo("Worthwhile Treasures 1.1.0 loaded.");}
  void OnDestroy(){if(harmony!=null)harmony.UnpatchSelf();if(chime)Destroy(chime);}
  static void Audit(Container chest,ZNetView view,string stage,string reason) { if(instance==null || !instance.diagnosticLogging.Value)return;
   try {
    string raw=chest.gameObject.name;
    string canonical=view?Utils.GetPrefabName(view.gameObject):"";
    if(raw.IndexOf("TreasureChest",StringComparison.OrdinalIgnoreCase)<0 && canonical.IndexOf("TreasureChest",StringComparison.OrdinalIgnoreCase)<0)return;
    var zdo=view?view.GetZDO():null;
    var inv=(Inventory)InventoryField.GetValue(chest);
    int coins=0,items=0;string content="";
    if(inv!=null)foreach(var item in inv.GetAllItems()) {
     items++;string name=item.m_dropPrefab?item.m_dropPrefab.name:item.m_shared.m_name;
     if(name=="Coins" || item.m_shared.m_name=="$item_coins")coins+=item.m_stack;
     content+=name+"x"+item.m_stack+";";
    }
    var piece=chest.GetComponent<Piece>();
    instance.Logger.LogInfo("CHEST AUDIT stage="+stage+" reason="+reason+" raw="+raw+" canonical="+canonical+
     " position="+chest.transform.position+" id="+(zdo==null?"none":zdo.m_uid.ToString())+
     " prefab="+(zdo==null?0:zdo.GetPrefab())+" owner="+(view && view.IsOwner())+
     " creator="+(piece?piece.GetCreator():0)+" applied="+(zdo!=null && zdo.GetBool(Applied,false))+
     " defaults="+(zdo!=null && zdo.GetBool(ZDOVars.s_addedDefaultItems,false))+
     " db="+(ObjectDB.instance!=null)+" slots="+(inv==null?"none":inv.GetWidth()+"x"+inv.GetHeight())+
     " coins="+coins+" items="+items+" contents="+content);
   }catch(Exception e){instance.Logger.LogWarning("Chest audit failed: "+e.Message);}
  }
  static void Reward(Container chest,ZNetView view) {
   if(!view || !view.IsOwner() || view.GetZDO()==null){Audit(chest,view,"reward-skip","network-or-owner");return;}
   var zdo=view.GetZDO();if(zdo.GetBool(Applied,false)){Audit(chest,view,"reward-skip","already-applied");return;}
   var piece=chest.GetComponent<Piece>();if(piece && piece.IsPlacedByPlayer()){Audit(chest,view,"reward-skip","player-created");return;}
   int tier=ChestIdentity.Tier(chest,view);if(tier==0){Audit(chest,view,"reward-skip","name-not-allowed");return;}
   // Do not call GetInventory: Epic Loot uses that accessor to roll deferred gear.
   var inv=(Inventory)InventoryField.GetValue(chest);if(inv==null || !ObjectDB.instance){Audit(chest,view,"reward-skip","inventory-or-db");return;}
   // Stage all changes before touching the real inventory. Preserve gear/custom data.
   var staged=new Inventory("Treasure staging",null,inv.GetWidth(),inv.GetHeight());
   var coins=ObjectDB.instance.GetItemPrefab("Coins");if(!coins){Audit(chest,view,"reward-skip","missing-coins-prefab");return;}
   string coinName=coins.GetComponent<ItemDrop>().m_itemData.m_shared.m_name;
   foreach(var item in inv.GetAllItems()) {
    if(item.m_shared.m_name==coinName && item.m_customData.Count==0)continue;
    if(!staged.AddItem(item.Clone())){Audit(chest,view,"reward-skip","staging-original-item-failed");return;}
   }
   bool jackpot=UnityEngine.Random.Range(0,100)<2;
   int amount=RewardPolicy.Coins(tier,jackpot,UnityEngine.Random.Range(0,201));
   string[] names={"Amber","AmberPearl","SilverNecklace"};
   // One saleable type per chest; modest quantities keep the coin reward central.
   int choice=UnityEngine.Random.Range(0,names.Length);
   var valuable=ObjectDB.instance.GetItemPrefab(names[choice]);
   int count=choice==2?1:UnityEngine.Random.Range(1,4);
   if(!valuable || !AddFullAmount(staged,coins,amount) || !AddFullAmount(staged,valuable,count)) {
    zdo.Set(Applied,true);instance.Logger.LogWarning("Treasure bonus skipped: insufficient space or missing item. Original loot preserved.");Audit(chest,view,"reward-skip","reward-capacity-or-prefab");return;
   }
   inv.GetAllItems().Clear();inv.GetAllItems().AddRange(staged.GetAllItems());
   AccessTools.Method(typeof(Inventory),"UpdateTotalWeight").Invoke(inv,null);
   Save.Invoke(chest,null);
   zdo.Set(Jackpot,jackpot);zdo.Set(Applied,true);
   instance.Logger.LogInfo("Treasure generated: "+chest.gameObject.name+", coins="+amount+", jackpot="+jackpot);
   Audit(chest,view,"reward-saved","success");
  }
  static bool AddFullAmount(Inventory inv,GameObject prefab,int count) {
   int max=prefab.GetComponent<ItemDrop>().m_itemData.m_shared.m_maxStackSize;
   if(max<1)return false;
   while(count>0){int chunk=Math.Min(max,count);if(!inv.AddItem(prefab,chunk))return false;count-=chunk;}
   return true;
  }
  static void Celebrate(Container chest,ZNetView view,bool granted) {
   if(!granted || !Player.m_localPlayer || !view || !view.IsOwner())return;
   var zdo=view.GetZDO();if(zdo==null || !zdo.GetBool(Jackpot,false)||zdo.GetBool(Heard,false))return;
   zdo.Set(Heard,true);
   Player.m_localPlayer.Message(MessageHud.MessageType.Center,"Treasure jackpot!");
   if(!chime) {
    const int rate=22050;var data=new float[rate];
    for(int i=0;i<data.Length;i++) {double t=(double)i/rate;double env=Math.Min(1,t*100)*Math.Exp(-5*t);data[i]=(float)(0.22*env*(Math.Sin(2*Math.PI*880*t)+0.5*Math.Sin(2*Math.PI*1320*t)));}
    chime=AudioClip.Create("Worthwhile Treasures chime",data.Length,1,rate,false);chime.SetData(data,0);
   }
   var go=new GameObject("Treasure chime");var source=go.AddComponent<AudioSource>();
   source.spatialBlend=0;source.volume=1;source.clip=chime;source.Play();Destroy(go,1.2f);
  }
  [HarmonyPatch(typeof(Container),"AddDefaultItems")]
  static class GeneratePatch {
   [HarmonyPrefix]static void Prefix(Container __instance,ZNetView ___m_nview){Audit(__instance,___m_nview,"defaults-before","entry");}
   [HarmonyPostfix,HarmonyPriority(Priority.Last),HarmonyAfter("randyknapp.mods.epicloot")]
   static void Postfix(Container __instance,ZNetView ___m_nview){try{Reward(__instance,___m_nview);}catch(Exception e){instance.Logger.LogError("Treasure generation failed: "+e);}}
  }
  [HarmonyPatch(typeof(Container),"RPC_OpenResponse")]
  static class OpenPatch {
   [HarmonyPostfix]static void Postfix(Container __instance,ZNetView ___m_nview,bool __1){Audit(__instance,___m_nview,"open-response",__1?"granted":"denied");try{Celebrate(__instance,___m_nview,__1);}catch(Exception e){instance.Logger.LogWarning("Treasure chime failed: "+e.Message);}}
  }
  [HarmonyPatch(typeof(Container),"Awake")]
  static class AwakeAudit {
   [HarmonyPostfix,HarmonyPriority(Priority.Last)]static void Postfix(Container __instance,ZNetView ___m_nview){Audit(__instance,___m_nview,"awake-complete","observed");}
  }
  [HarmonyPatch(typeof(Container),"RPC_RequestOpen")]
  static class RequestAudit {
   [HarmonyPrefix]static void Prefix(Container __instance,ZNetView ___m_nview){Audit(__instance,___m_nview,"open-request","observed");}
  }
 }
}
