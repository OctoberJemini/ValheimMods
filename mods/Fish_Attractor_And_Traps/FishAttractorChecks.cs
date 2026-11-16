using System;
using System.Collections.Generic;
using OctoberJemini.FishAttractor;
class FishAttractorChecks {
 static int count;
 static void Check(bool ok,string reason){count++;if(!ok)throw new Exception(reason);}
 static void Main(){
  foreach(var meat in BaitPolicy.DefaultMeats)Check(BaitPolicy.Allowed(meat,""),"Raw input recognized: "+meat);
  foreach(var food in new[]{"CookedMeat","Sausages","FishCooked","SerpentStew","Carrot","Resin",""})Check(!BaitPolicy.Allowed(food,""),"Reject cooked food and non-meat: "+food);
  Check(BaitPolicy.Allowed("ModdedRawMeat","Other, ModdedRawMeat"),"Extra raw meat configured");
  var bait=new List<string>{"RawMeat","SerpentMeat","NeckTail"};var loaded=BaitPolicy.Read(BaitPolicy.Write(bait));Check(string.Join(",",loaded.ToArray())=="RawMeat,SerpentMeat,NeckTail","Meat identity and order survive save/reload");
  Check(BaitPolicy.Read("").Count==0,"Empty station round trip");
  Check(!BaitPolicy.Ready(0,0,3,600,300),"No fish without bait");
  Check(!BaitPolicy.Ready(20,3,3,600,300),"Full local fish population prevents arrival");
  Check(!BaitPolicy.Ready(20,0,3,299,300),"Timer prevents early arrival");
  Check(BaitPolicy.Ready(20,2,3,300,300),"Fish can arrive exactly when due below local cap");
  Check(!BaitPolicy.Ready(20,0,3,300,0),"Uninitialized clock does not spawn");
  Check(BaitPolicy.WaterSuitable(30,28,1.5f,32),"A low dock over deep water is valid");
  Check(!BaitPolicy.WaterSuitable(30,29,1.5f,32),"Shallow water rejected");
  Check(!BaitPolicy.WaterSuitable(30,28,1.5f,35),"High inland station rejected");
  Check(!BaitPolicy.WaterSuitable(-10000,-10010,1.5f,-10000),"Missing water rejected");
  Check(!BaitPolicy.WaterSuitable(float.NaN,20,1.5f,30),"Invalid water rejected");
  Console.WriteLine("Passed "+count+" bait, persistence, timing, cap and water checks.");
 }
}
