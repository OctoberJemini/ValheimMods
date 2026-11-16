using System;
using OctoberJemini.FishAttractor;
class TrapChecks {
 static int count;
 static void Check(bool ok,string reason){count++;if(!ok)throw new Exception(reason);}
 static void Main(){
  Check(TrapRules.CanCatch(19,20,false,true,true,300,300),"One catch fits at cap boundary");
  Check(!TrapRules.CanCatch(20,20,false,true,true,300,300),"Full trap pauses");
  Check(!TrapRules.CanCatch(0,20,true,true,true,300,300),"Open container pauses");
  Check(!TrapRules.CanCatch(0,20,false,false,true,300,300),"No inventory space pauses");
  Check(!TrapRules.CanCatch(0,20,false,true,false,300,300),"No bait pauses");
  Check(!TrapRules.CanCatch(0,20,false,true,true,299,300),"Early catch rejected");
  Check(!TrapRules.CanCatch(0,20,false,true,true,300,0),"Uninitialized timer rejected");
  Check(TrapRules.CanCatch(0,20,false,true,true,999999,300),"Overdue trap permits one catch");
  Check(!TrapRules.CanCatch(1,20,false,true,true,999999,1000000),"Next due prevents an immediate catch-up burst");
  Check(TrapRules.Choose(0,new float[]{1,1})==0,"First ordinary fish");
  Check(TrapRules.Choose(.499f,new float[]{1,1})==0,"Lower half of ordinary bait distribution");
  Check(TrapRules.Choose(.5f,new float[]{1,1})==1,"Upper half of ordinary bait distribution");
  Check(TrapRules.Choose(1,new float[]{1,1})==1,"Unity random endpoint is valid");
  Check(TrapRules.Choose(.3f,new float[]{0,1,0})==1,"Unavailable fish skipped");
  Check(TrapRules.Choose(.5f,new float[]{0,0})==-1,"No eligible fish rejected");
  Check(TrapRules.Choose(.5f,new float[]{})==-1,"Empty bait match rejected");
  Check(TrapRules.Choose(.249f,new float[]{1,3})==0,"Native relative weight retained");
  Check(TrapRules.Choose(.25f,new float[]{1,3})==1,"Weighted threshold retained");
  Check(TrapRules.Choose(.9f,new float[]{1})==0,"Single specialty fish always selected");
  Console.WriteLine("Passed "+count+" trap timing, capacity, interaction and bait-weight checks.");
 }
}
