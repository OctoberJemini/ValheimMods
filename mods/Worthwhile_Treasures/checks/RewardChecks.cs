using System;
using W=OctoberJemini.WorthwhileTreasures.ChestPolicy;
using A=OctoberJemini.ArmorInscriptions.ChestPolicy;
using OctoberJemini.WorthwhileTreasures;
class RewardChecks {
 static int count;
 static void Check(bool ok,string why){count++;if(!ok)throw new Exception(why);}
 static void Main(){
  string[][] names={new[]{"TreasureChest_meadows","TreasureChest_blackforest","TreasureChest_forestcrypt","TreasureChest_fCrypt","TreasureChest_trollcave"},new[]{"TreasureChest_sunkencrypt","TreasureChest_swamp"},new[]{"TreasureChest_mountaincave","TreasureChest_mountains"},new[]{"TreasureChest_plains_stone","TreasureChest_heath"},new[]{"TreasureChest_dvergrtown","TreasureChest_dvergrtower","TreasureChest_dvergr_loose_stone"},new[]{"TreasureChest_charredfortress","TreasureChest_ashland_stone"},new[]{"TreasureChest_deepnorth_village"}};
  for(int t=1;t<=7;t++)foreach(string name in names[t-1])foreach(string suffix in new[]{"","(Clone)"," (1)(Clone)"," (2)(Clone)"," (12) (Clone)","(Clone)(Clone)"}) {
   Check(W.Tier(name+suffix,7)==t,"Gold dungeon classification "+name+suffix);
   Check(A.Tier(name+suffix,7)==t,"Patch dungeon classification "+name+suffix);
  }
  foreach(string name in new[]{"piece_chest_wood","piece_chest_private","piece_chest_blackmetal","piece_dvergr_crate","PlayerChest","ship","","NotTreasureChest_sunkencrypt"}) {
   Check(W.Tier(name,7)==0&&A.Tier(name,7)==0,"Exclude storage/resource "+name);
  }
  int[] lo={0,25,50,75,100,100,150,200},hi={0,75,100,150,200,300,350,400},jack={0,250,400,600,800,1000,1250,1500};
  for(int t=1;t<=7;t++) {
   Check(W.Tier("TreasureChest_modded_dungeon (3)(Clone)",t)==t&&A.Tier("TreasureChest_modded_dungeon (3)(Clone)",t)==t,"Modded fallback");
   for(int r=0;r<=200;r++) {
    int n=RewardPolicy.Coins(t,false,r);
    Check(n>=lo[t]&&n<=hi[t],"Range");Check(RewardPolicy.Coins(t,true,r)==jack[t],"Jackpot");
   }
   Check(RewardPolicy.Coins(t,false,0)==lo[t]&&RewardPolicy.Coins(t,false,200)==hi[t],"Endpoints");
   int wins=0;
   for(int r=0;r<100;r++) {if(A.PatchWins(t,r,true))wins++;Check(!A.PatchWins(t,r,false),"Before Eikthyr");}
   Check(wins==t,"Exact percentage");
  }
  Check(!A.PatchWins(0,0,true)&&!A.PatchWins(8,0,true),"Invalid tiers");
  Console.WriteLine(count+" shared dungeon classification, reward and patch probability checks passed.");
 }
}
