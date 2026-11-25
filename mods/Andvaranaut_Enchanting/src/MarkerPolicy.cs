using System;
using System.Collections.Generic;
namespace OctoberJemini.AndvaranautEnchanting {
 internal static class MarkerPolicy {
  internal static int[] MarkerIndices(IList<string> effects){
   var result=new List<int>();for(int i=effects.Count-1;i>=0;i--)if(effects[i]=="Andvaranaut")result.Add(i);return result.ToArray();
  }
  internal static bool EmptyOriginal(int effects,int sockets,int socketed,string legendary,string set,bool unidentified,string name){return effects==0&&sockets==0&&socketed==0&&String.IsNullOrEmpty(legendary)&&String.IsNullOrEmpty(set)&&!unidentified&&String.IsNullOrEmpty(name);}
 }
}
