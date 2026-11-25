using System;
using System.Collections.Generic;
using OctoberJemini.ArmorInscriptions;
class PolicyChecks{
 static int checks;
 static void Check(bool value,string name){checks++;if(!value)throw new Exception(name);}
 static Dictionary<string,string> Data(){return new Dictionary<string,string>{{"randyknapp.mods.epicloot#EpicLoot.MagicItemComponent","original-magic-and-sockets"},{"other_mod","keep-me"}};}
 static void Main(){
  for(int mask=0;mask<128;mask++){
   int expected=(mask&1)==0?0:500;
   Check(Rules.Price(key=>(mask&(1<<Array.IndexOf(Rules.BossKeys,key)))!=0)==expected,"fixed price after Eikthyr "+mask);
  }
  foreach(string name in new[]{"ArmorDeepNorthHeavyChest","ArmorDeepNorthHeavylegs","ArmorDeepNorthMediumChest","ArmorDeepNorthMediumlegs","ArmorDeepNorthMageChest","ArmorDeepNorthMagelegs"})
   Check(ArmorTiers.Cost(name)==7,"Deep North "+name);
  Check(ArmorTiers.Cost("ArmorBronzeChest")==1,"Black Forest cost");
  Check(ArmorTiers.Cost("ArmorRootLegs")==2,"Swamp cost");
  Check(ArmorTiers.Cost("ArmorFenringChest")==3,"Mountains cost");
  Check(ArmorTiers.Cost("ArmorPaddedGreaves")==4,"Plains cost");
  Check(ArmorTiers.Cost("ArmorMageChest")==5,"Mistlands cost");
  Check(ArmorTiers.Cost("ArmorAshlandsMediumlegs")==6,"Ashlands cost");
  Check(ArmorTiers.Cost("UnknownModArmor")==0,"unknown armor fails closed");
  Check(!ArmorTiers.Override("UnknownModArmor",0)&&!ArmorTiers.Override("UnknownModArmor",8),"invalid override rejected");
  Check(ArmorTiers.Override("UnknownModArmor",7)&&ArmorTiers.Cost("UnknownModArmor")==7,"modded tier override");
  for(int a=0;a<9;a++)for(int b=0;b<9;b++)for(int need=1;need<=7;need++){
   int[] stacks={a,b};int[] plan=ArmorTiers.Plan(stacks,need);
   Check((plan!=null)==(a+b>=need),"insufficient patches rejected");
   Check(stacks[0]==a&&stacks[1]==b,"planning does not mutate stacks");
   if(plan!=null)Check(plan[0]+plan[1]==need&&plan[0]<=a&&plan[1]<=b,"exact multi-stack cost");
  }
  Check(ArmorTiers.Plan(new[]{20},0)==null,"zero charge rejected");
  foreach(string id in Rules.Ids){
   var d=Data();int consumed=0;
   Check(Rules.TryApply(d,id,true,true,()=>{consumed++;return true;}),"apply "+id);
   Check(consumed==1&&Rules.Has(d,id),"one patch and persistent key");
   Check(d["other_mod"]=="keep-me"&&d["randyknapp.mods.epicloot#EpicLoot.MagicItemComponent"]=="original-magic-and-sockets","foreign metadata preserved");
   Check(!Rules.TryApply(d,id,true,true,()=>{consumed++;return true;})&&consumed==1,"duplicate consumes nothing");
   var failed=Data();
   Check(!Rules.TryApply(failed,id,false,true,()=>{throw new Exception("must not consume");}),"wrong armor slot");
   Check(!Rules.TryApply(failed,id,true,false,()=>{throw new Exception("must not consume");}),"stacked armor");
   Check(!Rules.TryApply(failed,id,true,true,()=>false)&&!Rules.Has(failed,id),"failed consumption rolls back");
   failed[Rules.Key(id)]="legacy";
   try{Rules.TryApply(failed,id,true,true,()=>{throw new Exception("simulated");});}catch{}
   Check(failed[Rules.Key(id)]=="legacy"&&failed.Count==3,"exception preserves previous data");
  }
  foreach(string id in Rules.Ids)for(int tier=1;tier<=7;tier++){
   int expectedCost=id=="builder"||id=="cultivator"?1:tier;
   Check(Rules.PatchCost(id,tier)==expectedCost,"separate patch cost "+id+" tier "+tier);
   foreach(bool rack in new[]{false,true})foreach(bool tools in new[]{false,true}){
    bool expectedGate=tier<=3||(tier<=5?rack:rack&&tools);
    Check(ExtensionRules.Allows(tier,rack,tools)==expectedGate,"extensions stay armor-tier gated "+id+" tier "+tier);
   }
   Check(Rules.PatchCost(id,0)==0&&Rules.PatchCost(id,8)==0,"unknown armor cannot become cheap "+id);
  }
  Check(Rules.PatchCost("unknown",7)==0,"unknown inscription has no cost");
  var head=Data();
  Check(Rules.TryApply(head,"snorkel",true,true,()=>true)&&Rules.TryApply(head,"tidewalker",true,true,()=>true),"both helmet inscriptions coexist");
  Check(Rules.Has(head,"snorkel")&&Rules.Has(head,"tidewalker"),"both helmet keys persist");
  Check(Rules.SwimMultiplier(true)==1.25f&&Rules.SwimMultiplier(false)==1f,"speed bonus exactly 25 percent and disabled fallback");
  foreach(string id in Rules.Ids)Check(Rules.IsHelmet(id)==(id=="snorkel"||id=="tidewalker"),"helmet slot isolation "+id);
  foreach(string name in new[]{"HelmetDNHeavy","HelmetDNMediumHood","HelmetDNMage"})Check(ArmorTiers.Cost(name)==7,"Deep North helmet "+name);
  string[] helmets={"HelmetLeather","HelmetRoot","HelmetFenring","HelmetPadded","HelmetMage","HelmetFlametal","HelmetDNMage"};
  for(int i=0;i<helmets.Length;i++)Check(ArmorTiers.Cost(helmets[i])==i+1,"helmet progression "+helmets[i]);
  var both=Data();Check(Rules.TryApply(both,"runner",true,true,()=>true)&&Rules.TryApply(both,"leaper",true,true,()=>true),"two leg patches coexist");
  Check(Rules.Has(both,"runner")&&Rules.Has(both,"leaper"),"both stay present");
  Check(!Rules.TryApply(both,"fishing",true,true,()=>true),"unknown inscription rejected");
  Check(Rules.ToolInscription("Hammer")=="builder","hammer mapping");
  Check(Rules.ToolInscription("Hoe")=="cultivator"&&Rules.ToolInscription("Cultivator")=="cultivator","hoe and cultivator mapping");
  Check(Rules.ToolInscription("SwordBlackmetal")==null&&Rules.ToolInscription("PickaxeBlackMetal")==null,"combat and mining excluded");
  for(int tier=0;tier<=8;tier++)foreach(bool rack in new[]{false,true})foreach(bool tools in new[]{false,true}){
   bool expected=tier>=1&&tier<=7&&(tier<=3||(tier<=5?rack:rack&&tools));
   Check(ExtensionRules.Allows(tier,rack,tools)==expected,"extension progression "+tier+" "+rack+" "+tools);
  }
  Console.WriteLine("Passed "+checks+" policy checks.");
 }
}
