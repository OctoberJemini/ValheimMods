using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace OctoberJemini.PortalAtlas
{
    // Engine-independent save data and naming rules, also exercised by the checks harness.
    [Serializable] public sealed class AtlasWorldData
    {
        public int version = 1;
        public List<BiomeCounter> counters = new List<BiomeCounter>();
        public List<AtlasName> names = new List<AtlasName>();

        public void Observe(string biome, int number)
        {
            var counter = counters.FirstOrDefault(c => c.biome == biome);
            if (counter == null) { counter = new BiomeCounter { biome = biome }; counters.Add(counter); }
            counter.last = Math.Max(counter.last, number);
        }
        public int Next(string biome)
        {
            Observe(biome, 0);
            var counter = counters.First(c => c.biome == biome);
            counter.last = checked(counter.last + 1);
            return counter.last;
        }
        public AtlasName Find(string id) { return names.FirstOrDefault(n => n.id == id); }
        public AtlasName Assign(string id, string biome, int savedNumber, string savedName)
        {
            var record = Find(id);
            if (record != null) { Observe(record.biome, record.number); return record; }
            var number = savedNumber > 0 ? savedNumber : Next(biome);
            Observe(biome, number);
            record = new AtlasName { id = id, biome = biome, number = number, label = Clean(savedName) };
            names.Add(record);
            return record;
        }
        public static string DefaultName(string biome, int number) { return "Atlas-" + number.ToString("D3", CultureInfo.InvariantCulture); }
        public static string WithBiome(string name,string biome)
        {
            var suffix=" ("+biome+")";
            return (name??"").EndsWith(suffix,StringComparison.OrdinalIgnoreCase)?name:(name??"")+suffix;
        }
        public static string Clean(string value)
        {
            if (String.IsNullOrEmpty(value)) return "";
            return new string(value.Where(c => !Char.IsControl(c) && c != '<' && c != '>').Take(48).ToArray()).Trim();
        }
        public static bool Search(string label, string biome, string query)
        {
            return String.IsNullOrWhiteSpace(query) || ((label ?? "") + " " + (biome ?? "")).IndexOf(query.Trim(), StringComparison.OrdinalIgnoreCase) >= 0;
        }
    }
    [Serializable] public sealed class BiomeCounter { public string biome = ""; public int last; }
    [Serializable] public sealed class AtlasName
    {
        public string id = "";
        public string biome = "";
        public int number;
        public string label = "";
        public string Display { get { return String.IsNullOrEmpty(label) ? AtlasWorldData.DefaultName(biome, number) : label; } }
    }
}
