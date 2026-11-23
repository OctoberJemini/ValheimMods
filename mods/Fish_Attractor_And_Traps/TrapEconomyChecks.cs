using System;
using System.Linq;
using Newtonsoft.Json;
using OctoberJemini.FishAttractor;
class TrapEconomyChecks {
 static int checks;
 static void Check(bool value,string reason){checks++;if(!value)throw new Exception(reason);}
 static TrapAttempt Step(TrapLedger l,string id,long ack,double time,float skill,bool ready,double loss,out string status,float x=0){return TrapEconomy.Step(l,id,ack,x,0,0,time,skill,ready,"bait",new[]{"Fish1"},new[]{1f},loss,0,out status);}
 static void Main(){
  Check(TrapEconomy.Interval(0)==900,"beginner 15 minutes");Check(TrapEconomy.Interval(50)==720,"mid 12 minutes");Check(TrapEconomy.Interval(100)==600,"expert 10 minutes");
  Check(Math.Abs(TrapEconomy.Loss(0)-.25)<.0001,"beginner 25 percent");Check(Math.Abs(TrapEconomy.Loss(50)-.15)<.0001,"mid 15 percent");Check(Math.Abs(TrapEconomy.Loss(100)-.05)<.0001,"expert 5 percent");
  Check(TrapEconomy.Interval(-10)==900&&TrapEconomy.Interval(150)==600,"clamped skill");Check(TrapEconomy.Interval(float.NaN)==900,"invalid skill");
  Check(TrapEconomy.Nearby(0,0,0,20,0,0),"20m inclusive");Check(!TrapEconomy.Nearby(0,0,0,20.01f,0,0),"outside 20m independent");Check(!TrapEconomy.Nearby(0,0,0,0,0,5000),"cave not same surface pool");
  Check(TrapEconomy.Eligible(true,false,3,1),"native biome unlock-free catch");Check(!TrapEconomy.Eligible(true,false,3,4),"undiscovered outside native biome blocked");Check(TrapEconomy.Eligible(true,true,3,4),"discovered cross-biome catch");Check(!TrapEconomy.Eligible(false,true,3,4),"discovery does not bypass bait");Check(!TrapEconomy.Eligible(true,false,0,4),"unknown habitat fails closed");
  Check(!TrapEconomy.UnlockOnCollection(0,1,0),"deposited/bought fish lack catch credit");Check(TrapEconomy.UnlockOnCollection(0,1,1),"collected real trap catch unlocks");Check(!TrapEconomy.UnlockOnCollection(1,1,1),"failed transfer cannot unlock");Check(!TrapEconomy.UnlockOnCollection(2,1,1),"deposit cannot unlock");
  var l=new TrapLedger();string status;
  var a=Step(l,"one",0,100,0,false,.9,out status);Check(a.Due==0&&!a.Pending,"invalid trap does not start timer");
  a=Step(l,"one",0,100,0,true,.9,out status);Check(a.Due==1000&&!a.Pending,"first catch full interval");
  a=Step(l,"one",0,999,0,true,.9,out status);Check(!a.Pending,"not early");
  a=Step(l,"one",0,1000,0,true,.9,out status);Check(a.Pending&&a.Sequence==1&&a.Fish=="Fish1"&&!a.Lost,"successful catch receipt");Check(TrapEconomy.Available(l,0,0,0,1000)==2,"success spends stock");
  a=Step(l,"one",0,1100,0,true,0,out status);Check(a.Sequence==1&&!a.Lost&&l.Harvests.Count==1,"retry replays same result; no reroll/debit");
  a=Step(l,"one",1,1100,0,true,.9,out status);Check(!a.Pending&&a.Due==2000,"acknowledgment schedules next full interval");
  a=Step(l,"one",1,2000,0,true,0,out status);Check(a.Pending&&a.Lost&&a.Fish=="","failure receipt");Check(l.Harvests.Count==1,"lost bait does not consume stock");
  l=JsonConvert.DeserializeObject<TrapLedger>(JsonConvert.SerializeObject(l));a=Step(l,"one",1,2100,0,true,.9,out status);Check(a.Lost&&a.Sequence==2,"save/load preserves pending failure, no reroll");
  l.Harvests.Clear();for(int i=0;i<3;i++)l.Harvests.Add(new TrapHarvest{X=0,Z=0,Y=0,Time=2000});
  a=Step(l,"newtrap",0,2000,100,true,.9,out status,19);a=Step(l,"newtrap",0,2600,100,true,0,out status,19);Check(!a.Pending&&status.Contains("scarce"),"nearby rebuilt trap cannot bypass depletion or lose bait");
  Check(TrapEconomy.Available(l,20.01f,0,0,2600)==3,"distant trap has independent stock");Check(TrapEconomy.Available(l,0,0,0,3799)==0,"stock not early");Check(TrapEconomy.Available(l,0,0,0,3800)==3,"stock returns after 30 minutes");
  a=Step(l,"newtrap",0,3800,100,true,.9,out status,19);Check(a.Pending&&!a.Lost,"recovered stock permits catch");
  a=Step(l,"newtrap",1,9000,100,true,.9,out status,19);Check(!a.Pending&&a.Due==9600,"delayed receipt acknowledgment has no catch-up burst");
  bool threw=false;try{Step(l,"newtrap",100,10000,100,true,.9,out status);}catch(InvalidOperationException){threw=true;}Check(threw,"mismatched restored world fails closed");
  var candidates=TrapRules.Choose(.5f,new float[0]);Check(candidates==-1,"no eligible species");
  for(int skill=0;skill<=100;skill++)Check(TrapEconomy.Interval(skill)>=600&&TrapEconomy.Interval(skill)<=900&&TrapEconomy.Loss(skill)>=.049999&&TrapEconomy.Loss(skill)<=.25,"bounded skill curve");
  Console.WriteLine("PASS: "+checks+" fishing economy, persistence and concurrency-policy checks.");
 }
}
