using System;

namespace OctoberJemini.SafeItemStacks {
 internal static class StackPolicy {
  internal static int Limit(string prefab, string token, int current, int eggs, int seals, int mead, int utility=50) {
   if(token=="$item_dragonegg")return eggs;
   // Dropped instances can use Unity's clone suffix before their prefab reference is assigned.
   if(prefab!=null && prefab.EndsWith("(Clone)",StringComparison.Ordinal))
    prefab=prefab.Substring(0,prefab.Length-7);
   if(prefab=="DvergrKey")return Math.Max(current,seals);
   if(prefab=="BarleyWineBase" || (prefab!=null && prefab.StartsWith("MeadBase",StringComparison.Ordinal)))return Math.Max(current,mead);
   switch(prefab) {
    case "CryptKey":
    case "SaddleLox": case "SaddleAsksvin": case "SaddleMoose":
     return Math.Max(current,utility);
   }
   return current;
  }
 }
}
