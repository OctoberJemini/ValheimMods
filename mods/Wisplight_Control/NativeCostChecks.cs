using System;
using System.Collections;
using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;
using HarmonyLib;

class NativeCostChecks
{
    static int count;
    static void Check(bool ok, string label) { count++; if (!ok) throw new Exception(label); }
    static void Main()
    {
        AppDomain.CurrentDomain.AssemblyResolve += (s, e) => {
            string name = new AssemblyName(e.Name).Name + ".dll";
            foreach (string root in new[] { Environment.GetEnvironmentVariable("WISP_GAME_MANAGED"), Environment.GetEnvironmentVariable("WISP_BEP_CORE"), AppDomain.CurrentDomain.BaseDirectory })
            { string path = Path.Combine(root, name); if (File.Exists(path)) return Assembly.LoadFrom(path); }
            return null;
        };
        Run();
    }
    [MethodImpl(MethodImplOptions.NoInlining)]
    static void Run()
    {
        var mod = Assembly.LoadFrom(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "WisplightControl.dll"));
        var tiered = mod.GetType("OctoberJemini.WisplightControl.TieredUpgradeCosts", true);
        var owned = (IDictionary)tiered.GetField("Owned", BindingFlags.NonPublic | BindingFlags.Static).GetValue(null);
        var patch = tiered.GetNestedType("AmountPatch", BindingFlags.NonPublic);
        var harmony = new Harmony("OctoberJemini.WispCostChecks");
        harmony.CreateClassProcessor(patch).Patch();
        try
        {
            string[] names = { "Wisp", "Silver", "Eitr", "TrophyGjall", "TrophySeekerBrute" };
            int[,] expected = { {1,20,20,20}, {1,4,8,16}, {0,10,25,0}, {0,0,1,0}, {0,0,0,1} };
            var getter = typeof(Piece.Requirement).GetMethod("GetAmount");
            for (int item = 0; item < names.Length; item++)
            {
                var req = new Piece.Requirement { m_amount = item < 2 ? 1 : 0, m_amountPerLevel = 6 };
                owned.Add(req, names[item]);
                for (int quality = 1; quality <= 4; quality++)
                    Check((int)getter.Invoke(req, new object[] {quality}) == expected[item,quality-1], names[item] + " quality " + quality);
            }
            var other = new Piece.Requirement { m_amount = 7, m_amountPerLevel = 2 };
            Check((int)getter.Invoke(other,new object[]{1})==7,"Other recipe crafting unchanged");
            Check((int)getter.Invoke(other,new object[]{4})==8,"Other recipe native Q4 multiplier unchanged");
            owned.Clear();
            Check((int)getter.Invoke(other,new object[]{2})==2,"Removing ownership restores native handling");
            System.Console.WriteLine("Passed " + count + " checks through the patched native Requirement.GetAmount, including recipe isolation.");
        }
        finally { owned.Clear(); harmony.UnpatchSelf(); }
    }
}
