using System;
using System.Collections.Generic;
using System.IO;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace OctoberJemini.PortalAtlas
{
    internal static class FavouriteStore
    {
        private const string Header="Portal Atlas favourites v2";
        // ZDO IDs are reassigned during world loading. Static destinations instead use
        // their type and centimetre-quantized world position, scoped by character/world.
        internal static string Key(bool pillar,float x,float y,float z)
        {
            return (pillar?"pillar":"portal")+":"+Coordinate(x)+":"+Coordinate(y)+":"+Coordinate(z);
        }
        private static string Coordinate(float value)
        {
            if(Single.IsNaN(value) || Single.IsInfinity(value))throw new ArgumentException("Invalid destination position");
            return Math.Round((double)value*100,MidpointRounding.AwayFromZero).ToString("0",CultureInfo.InvariantCulture);
        }
        internal static HashSet<string> Read(string path)
        {
            if(!File.Exists(path))return null;
            var lines=File.ReadAllLines(path,Encoding.UTF8);
            if(lines.Length==0 || lines[0]!=Header)throw new InvalidDataException("Unknown favourites file format");
            var result=new HashSet<string>(StringComparer.Ordinal);
            foreach(var line in lines.Skip(1))
            {
                if(line.Length==0)continue;
                if(line.Length>100 || !Regex.IsMatch(line,@"^(portal|pillar):-?\d+:-?\d+:-?\d+$"))throw new InvalidDataException("Invalid destination ID in favourites");
                result.Add(line);
            }
            return result;
        }
        internal static void Save(string path,IEnumerable<string> ids)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            var text=Header+"\n"+String.Join("\n",ids.Distinct().OrderBy(id=>id,StringComparer.Ordinal))+"\n";
            var temporary=path+".tmp";File.WriteAllText(temporary,text,new UTF8Encoding(false));
            if(File.Exists(path))File.Replace(temporary,path,path+".bak");else File.Move(temporary,path);
        }
    }
}
