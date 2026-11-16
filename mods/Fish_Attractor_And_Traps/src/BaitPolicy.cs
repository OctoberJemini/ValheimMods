using System;
using System.Collections.Generic;
namespace OctoberJemini.FishAttractor {
 public static class BaitPolicy {
  public static readonly string[] DefaultMeats={"RawMeat","DeerMeat","NeckTail","WolfMeat","LoxMeat","HareMeat","ChickenMeat","SerpentMeat","SeekerMeat","AsksvinMeat","VoltureMeat","FishRaw"};
  public static bool Allowed(string prefab,string extras){
   foreach(var meat in DefaultMeats)if(string.Equals(prefab,meat,StringComparison.Ordinal))return true;
   foreach(var meat in (extras??"").Split(','))if(meat.Trim().Length>0&&string.Equals(prefab,meat.Trim(),StringComparison.Ordinal))return true;
   return false;
  }
  public static List<string> Read(string stored){var result=new List<string>();foreach(var s in (stored??"").Split('|'))if(s.Length>0)result.Add(s);return result;}
  public static string Write(List<string> bait){return string.Join("|",bait.ToArray());}
  public static bool Ready(int bait,int nearby,int limit,long now,long due){return bait>0&&nearby<limit&&due>0&&now>=due;}
  public static bool WaterSuitable(float level,float ground,float depth,float stationY){return !float.IsNaN(level)&&!float.IsInfinity(level)&&level>-1000&&level-ground>=depth&&Math.Abs(stationY-level)<=4;}
 }
}
