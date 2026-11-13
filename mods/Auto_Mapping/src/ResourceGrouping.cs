using System;
using System.Collections.Generic;
using System.Linq;
namespace OctoberJemini.QuietMap {
 internal static class ResourceGrouping {
  internal static Dictionary<string,List<Record>> Build(IEnumerable<Record> input){
   var result=new Dictionary<string,List<Record>>();
   foreach(var kind in input.GroupBy(r=>new{r.Kind,Name=r.Kind=="Ore"?"":Policy.IsTar(r)?"$tar":r.Name})){
    if(!Policy.Small(kind.Key.Kind)){foreach(var r in kind)result.Add(r.Key,new List<Record>{r});continue;}
    float distance=(kind.Key.Kind=="Ore"||kind.Key.Name=="$tar")?32:12;
    float height=(kind.Key.Kind=="Ore"||kind.Key.Name=="$tar")?12:4;
    var list=kind.OrderBy(r=>r.X).ThenBy(r=>r.Key,StringComparer.Ordinal).ToArray();
    var parent=Enumerable.Range(0,list.Length).ToArray();
    for(int i=0;i<list.Length;i++)for(int j=i+1;j<list.Length;j++){
     float dx=list[j].X-list[i].X;if(dx>distance)break;
     float dz=list[j].Z-list[i].Z;
     if(Math.Abs(list[j].Y-list[i].Y)>height||dx*dx+dz*dz>distance*distance)continue;
     int a=Root(parent,i),b=Root(parent,j);if(a!=b)parent[b]=a;
    }
    foreach(var cluster in Enumerable.Range(0,list.Length).GroupBy(i=>Root(parent,i))){
     var members=cluster.Select(i=>list[i]).OrderBy(r=>r.Key,StringComparer.Ordinal).ToList();
     result.Add("cluster:"+members[0].Key,members);
    }
   }
   return result;
  }
  internal static string Label(Record first,int count)=>(first.Kind=="Ore"||Policy.IsTar(first))?(count>1?"×"+count:""):first.Name+(count>1?" x"+count:"");
  static int Root(int[] parent,int i){while(parent[i]!=i){parent[i]=parent[parent[i]];i=parent[i];}return i;}
 }
}
