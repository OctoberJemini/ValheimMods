namespace OctoberJemini.FishAttractor {
 public static class TrapRules {
  public static bool CanCatch(int count,int limit,bool inUse,bool hasSpace,bool hasBait,long now,long due){return hasBait&&!inUse&&hasSpace&&count<limit&&due>0&&now>=due;}
  public static int Choose(float unit,float[] weights){float sum=0;foreach(float w in weights)if(w>0)sum+=w;if(sum<=0)return -1;float target=unit*sum;for(int i=0;i<weights.Length;i++){if(weights[i]<=0)continue;target-=weights[i];if(target<0)return i;}for(int i=weights.Length-1;i>=0;i--)if(weights[i]>0)return i;return -1;}
 }
}
