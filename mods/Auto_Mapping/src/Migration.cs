using System;
using System.Globalization;
namespace OctoberJemini.QuietMap {
 internal static class Migration {
  internal static Record Read(string line){
   if(string.IsNullOrEmpty(line)||line.StartsWith("#"))return null;
   string[] cells=line.Split('\t');if(cells.Length<10)return null;
   string source=cells[0];string kind=source=="portal"?"Portal":source.StartsWith("place:")&&Policy.IsDungeonName(source.Substring(6))?"Dungeon":null;
   if(kind==null)return null;
   if(!float.TryParse(cells[2],NumberStyles.Float,CultureInfo.InvariantCulture,out var x)||!float.TryParse(cells[3],NumberStyles.Float,CultureInfo.InvariantCulture,out var y)||!float.TryParse(cells[4],NumberStyles.Float,CultureInfo.InvariantCulture,out var z))return null;
   if(float.IsNaN(x)||float.IsInfinity(x)||float.IsNaN(y)||float.IsInfinity(y)||float.IsNaN(z)||float.IsInfinity(z))return null;
   return new Record{Key="legacy:"+source+":"+cells[2]+":"+cells[4],Kind=kind,Name=string.IsNullOrWhiteSpace(cells[8])?kind:cells[8],Prefab=source.StartsWith("place:")?source.Substring(6):"portal",X=x,Y=y,Z=z,Available=!cells[6].Contains("E"),Deleted=cells[6].Contains("D"),Visited=cells[6].Contains("C")};
  }
 }
}
