using System;
using System.Collections.Generic;
using System.Linq;

namespace OctoberJemini.FishAttractor {
 public sealed class TrapProfile { public string Name=""; public float Skill; public HashSet<string> Fish=new HashSet<string>(StringComparer.Ordinal); }
 public sealed class TrapHarvest { public float X,Z,Y; public double Time; }
 public sealed class TrapAttempt {
  public long Sequence; public double Due; public string Fish="",Bait=""; public bool Lost,Pending;
 }
 public sealed class TrapLedger {
  public int Version=1;
  public Dictionary<long,TrapProfile> Profiles=new Dictionary<long,TrapProfile>();
  public Dictionary<string,TrapAttempt> Traps=new Dictionary<string,TrapAttempt>();
  public List<TrapHarvest> Harvests=new List<TrapHarvest>();
 }
 public static class TrapEconomy {
  public const float Range=20;
  public const int Stock=3;
  public const double Recovery=1800;
  public static float Skill(float value){return float.IsNaN(value)||float.IsInfinity(value)?0:Math.Max(0,Math.Min(100,value));}
  // Piecewise interpolation preserves the agreed 0/50/100 targets exactly.
  public static double Interval(float skill){double s=Skill(skill);return s<=50?900-s*3.6:720-(s-50)*2.4;}
  public static double Loss(float skill){return .25-Skill(skill)*.002;}
  public static bool Eligible(bool correctBait,bool discovered,int nativeBiomes,int currentBiome){return correctBait&&(discovered||(nativeBiomes&currentBiome)!=0);}
  public static bool UnlockOnCollection(int before,int after,int catchCredits){return after>before&&catchCredits>0;}
  public static bool Nearby(float x,float z,float y,float bx,float bz,float by){double dx=x-bx,dz=z-bz;return Math.Abs(y-by)<100 && dx*dx+dz*dz<=Range*Range;}
  public static int Available(TrapLedger ledger,float x,float z,float y,double now){return Math.Max(0,Stock-ledger.Harvests.Count(h=>h.Time>now-Recovery&&Nearby(x,z,y,h.X,h.Z,h.Y)));}
  public static TrapAttempt Step(TrapLedger ledger,string id,long ack,float x,float z,float y,double now,float skill,bool ready,string bait,string[] fish,float[] weights,double lossRoll,float fishRoll,out string status){
   ledger.Harvests.RemoveAll(h=>h.Time<=now-Recovery);
   TrapAttempt a;if(!ledger.Traps.TryGetValue(id,out a)){a=new TrapAttempt();ledger.Traps.Add(id,a);}
   if(ack>a.Sequence)throw new InvalidOperationException("Trap receipt is newer than the world ledger; restore matching saves.");
   if(a.Pending&&ack==a.Sequence){a.Pending=false;a.Due=now+Interval(skill);a.Fish="";a.Bait="";a.Lost=false;}
   if(a.Pending){status="Catch waiting for collection into trap";return a;}
   if(!ready||fish.Length==0){status="Waiting for suitable water, eligible bait, storage or owner data";return a;}
   if(a.Due==0){a.Due=now+Interval(skill);status="Fishing";return a;}
   if(now<a.Due){status="Fishing";return a;}
   if(Available(ledger,x,z,y,now)==0){status="Fish are scarce - waiting for recovery";return a;}
   int pick=TrapRules.Choose(fishRoll,weights);if(pick<0){status="No eligible fish";return a;}
   a.Sequence++;a.Pending=true;a.Bait=bait;a.Lost=lossRoll<Loss(skill);a.Fish=a.Lost?"":fish[pick];
   if(!a.Lost)ledger.Harvests.Add(new TrapHarvest{X=x,Z=z,Y=y,Time=now});
   status=a.Lost?"Bait lost - no catch":"Fish caught";return a;
  }
 }
}
