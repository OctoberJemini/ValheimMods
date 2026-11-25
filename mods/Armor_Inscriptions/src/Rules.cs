using System;
using System.Collections.Generic;
namespace OctoberJemini.ArmorInscriptions {
 internal static class Rules {
  internal const string Prefix="oct_armor_inscription_";
  internal static readonly string[] Ids={"runner","leaper","cultivator","builder","snorkel","tidewalker"};
  internal static readonly string[] BossKeys={"defeated_eikthyr","defeated_gdking","defeated_bonemass","defeated_dragon","defeated_goblinking","defeated_queen","defeated_fader"};
  internal static bool IsLegs(string id){return id=="runner"||id=="leaper";}
  internal static bool IsHelmet(string id){return id=="snorkel"||id=="tidewalker";}
  internal static float SwimMultiplier(bool active){return active?1.25f:1f;}
  internal static bool IsKnown(string id){return Array.IndexOf(Ids,id)>=0;}
  internal static int PatchCost(string id,int tier){
   if(!IsKnown(id)||tier<1||tier>7)return 0;
   return id=="builder"||id=="cultivator"?1:tier;
  }
  internal static string Key(string id){return Prefix+id+"_v1";}
  internal static bool Has(IDictionary<string,string> data,string id){string value;return data!=null&&data.TryGetValue(Key(id),out value)&&value=="1";}
  internal static int Price(Func<string,bool> defeated){return defeated("defeated_eikthyr")?500:0;}
  internal static string ToolInscription(string prefab){return prefab=="Hammer"?"builder":prefab=="Hoe"||prefab=="Cultivator"?"cultivator":null;}
  // Mutate only our own key; consume exactly one patch after recording the inscription.
  // A rejected consumption rolls back without touching another mod's item metadata.
  internal static bool TryApply(IDictionary<string,string> data,string id,bool correctSlot,bool singleArmor,Func<bool> consume){
   if(data==null||!IsKnown(id)||!correctSlot||!singleArmor||Has(data,id))return false;
   string old;bool existed=data.TryGetValue(Key(id),out old);
   data[Key(id)]="1";
   try {if(consume())return true;}
   catch {if(existed)data[Key(id)]=old;else data.Remove(Key(id));throw;}
   if(existed)data[Key(id)]=old;else data.Remove(Key(id));
   return false;
  }
 }
}
