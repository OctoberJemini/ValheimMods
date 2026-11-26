using System;
using OctoberJemini.SafeItemStacks;

class StackPolicyTests {
 static int count;
 static void Check(string prefab,string token,int current,int expected) {
  int actual=StackPolicy.Limit(prefab,token,current,50,40,60);
  if(actual!=expected)throw new Exception(prefab+": expected "+expected+", got "+actual);
  count++;
 }
 static void Main() {
  Check("DragonEgg","$item_dragonegg",1,50);
  Check("DvergrKey","$item_dvergrkey",1,40);
  Check("DvergrKeyFragment(Clone)","",9,9);
  Check("MeadBaseHealthMedium","",10,60);
  Check("MeadBaseTasty(Clone)","",1,60);
  Check("BarleyWineBase","",1,60);
  Check("MeadBaseHealthMajor","",100,100);
  Check("DvergrKey","",100,100);
  Check("MeadHealthMedium","",100,100);
  Check("SwordBlackmetal","",1,1);
  Check("CryptKey","",1,50);
  foreach(string p in new[]{"SaddleLox","SaddleAsksvin","SaddleMoose"})Check(p,"",1,50);
  foreach(string p in new[]{"DyrnwynBladeFragment","DyrnwynHiltFragment","DyrnwynTipFragment","Wishbone","Demister","BeltStrength","Tankard","TankardAnniversary","TankardOdin","Tankard_dvergr","TrinketSilverDamage"})Check(p,"",1,1);
  Check("SaddleLox(Clone)","",1,50);
  Check("Wishbone","",100,100);
  Check("ArmorIronChest","",1,1);
  Check("CapeFeather","",1,1);
  Check("HelmetDverger","",1,1);
  Check("Hammer","",1,1);
  Check("SwordDyrnwyn","",1,1);
  Check("NotMeadBaseHealth","",1,1);
  Check(null,null,1,1);
  Console.WriteLine("PASS: "+count+" stacking policy cases.");
 }
}
