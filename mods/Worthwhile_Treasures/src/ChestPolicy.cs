using System;
namespace OctoberJemini.WorthwhileTreasures {
 internal static class ChestPolicy {
  internal static string Normalize(string name) {
   if(string.IsNullOrEmpty(name))return "";
   name=name.Trim();
   while(name.EndsWith(")",StringComparison.Ordinal)) {
    int start=name.LastIndexOf('(');if(start<0)break;
    string suffix=name.Substring(start+1,name.Length-start-2);int number;
    if(suffix!="Clone"&&!int.TryParse(suffix,out number))break;
    name=name.Substring(0,start).TrimEnd();
   }
   return name;
  }
  internal static int Tier(string name,int biomeTier) {
   name=Normalize(name).ToLowerInvariant();
   if(!name.StartsWith("treasurechest_",StringComparison.Ordinal))return 0;
   if(name.Contains("deepnorth"))return 7;
   if(name.Contains("charred")||name.Contains("ashland"))return 6;
   if(name.Contains("dvergr")||name.Contains("mistland"))return 5;
   if(name.Contains("plains")||name.Contains("heath"))return 4;
   if(name.Contains("mountain"))return 3;
   if(name.Contains("sunkencrypt")||name.Contains("swamp"))return 2;
   if(name.Contains("forestcrypt")||name.Contains("fcrypt")||name.Contains("trollcave")||name.Contains("blackforest")||name.Contains("meadows"))return 1;
   return Math.Max(1,Math.Min(7,biomeTier));
  }
  internal static bool PatchWins(int tier,int roll,bool unlocked) {
   return unlocked&&tier>=1&&tier<=7&&roll>=0&&roll<100&&roll<tier;
  }
 }
}
