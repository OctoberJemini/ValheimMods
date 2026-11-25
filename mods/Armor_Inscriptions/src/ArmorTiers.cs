using System;
using System.Collections.Generic;
namespace OctoberJemini.ArmorInscriptions {
 internal static class ArmorTiers {
  private static readonly Dictionary<string,int> Costs=new Dictionary<string,int>(StringComparer.OrdinalIgnoreCase);
  static ArmorTiers(){
   Add(1,"ArmorRagsChest ArmorRagsLegs ArmorLeatherChest ArmorLeatherLegs ArmorTrollLeatherChest ArmorTrollLeatherLegs ArmorBronzeChest ArmorBronzeLegs ArmorBerserkerChest ArmorBerserkerLegs ArmorHarvester1 ArmorHarvester2");
   Add(2,"ArmorIronChest ArmorIronLegs ArmorRootChest ArmorRootLegs");
   Add(3,"ArmorWolfChest ArmorWolfLegs ArmorFenringChest ArmorFenringLegs");
   Add(4,"ArmorPaddedCuirass ArmorPaddedGreaves ArmorLoxChest ArmorLoxLegs ArmorBerserkerUndeadChest ArmorBerserkerUndeadLegs");
   Add(5,"ArmorCarapaceChest ArmorCarapaceLegs ArmorMageChest ArmorMageLegs");
   Add(6,"ArmorFlametalChest ArmorFlametalLegs ArmorAshlandsMediumChest ArmorAshlandsMediumlegs ArmorMageChest_Ashlands ArmorMageLegs_Ashlands");
   Add(7,"ArmorDeepNorthHeavyChest ArmorDeepNorthHeavylegs ArmorDeepNorthMediumChest ArmorDeepNorthMediumlegs ArmorDeepNorthMageChest ArmorDeepNorthMagelegs");
   Add(1,"HelmetLeather HelmetTrollLeather HelmetBronze HelmetBerserkerHood HelmetDverger HelmetYule HelmetOdin HelmetMidsummerCrown HelmetStrawHat HelmetSweatBand HelmetCelebration");
   Add(2,"HelmetIron HelmetRoot");
   Add(3,"HelmetDrake HelmetFenring");
   Add(4,"HelmetPadded HelmetLox HelmetBerserkerUndead");
   Add(5,"HelmetCarapace HelmetMage HelmetFishingHat");
   Add(6,"HelmetFlametal HelmetAshlandsMediumHood HelmetMage_Ashlands");
   Add(7,"HelmetDNHeavy HelmetDNMediumHood HelmetDNMage HelmetCrownofValheim");
   for(int n=1;n<=10;n++)Costs["HelmetHat"+n]=1;
   for(int n=1;n<=10;n++){Costs["ArmorTunic"+n]=1;Costs["ArmorDress"+n]=1;}
  }
  private static void Add(int cost,string names){foreach(string name in names.Split(' '))Costs[name]=cost;}
  internal static int Cost(string prefab){int cost;return prefab!=null&&Costs.TryGetValue(prefab,out cost)?cost:0;}
  internal static bool Override(string prefab,int cost){if(string.IsNullOrWhiteSpace(prefab)||cost<1||cost>7)return false;Costs[prefab.Trim()]=cost;return true;}
  internal static int[] Plan(int[] stacks,int needed){
   if(needed<1)return null;
   var plan=new int[stacks.Length];int left=needed;
   for(int i=0;i<stacks.Length;i++){if(stacks[i]<0)return null;plan[i]=Math.Min(stacks[i],left);left-=plan[i];}
   return left==0?plan:null;
  }
 }
}
