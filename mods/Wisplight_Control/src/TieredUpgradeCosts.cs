using System.Collections.Generic;
using HarmonyLib;

namespace OctoberJemini.WisplightControl
{
    internal static class TieredUpgradeCosts
    {
        private static readonly Dictionary<Recipe, Piece.Requirement[]> Originals = new Dictionary<Recipe, Piece.Requirement[]>();
        private static readonly Dictionary<Piece.Requirement, string> Owned = new Dictionary<Piece.Requirement, string>();

        internal static bool Configure(ObjectDB db, Recipe recipe)
        {
            if (Originals.ContainsKey(recipe)) return true;
            // Resolve every ingredient before changing the recipe; ObjectDB can initialize in stages.
            var extraItems = new List<ItemDrop>();
            foreach (var name in new[] { "Eitr", "TrophyGjall", "TrophySeekerBrute" })
            {
                var prefab = db.GetItemPrefab(name);
                var drop = prefab ? prefab.GetComponent<ItemDrop>() : null;
                if (!drop) return false;
                extraItems.Add(drop);
            }
            var resources = new List<Piece.Requirement>(recipe.m_resources);
            Originals.Add(recipe, recipe.m_resources);
            foreach (var requirement in resources)
                if (requirement.m_resItem && (requirement.m_resItem.name == "Wisp" || requirement.m_resItem.name == "Silver"))
                    Owned[requirement] = requirement.m_resItem.name;
            foreach (var item in extraItems)
            {
                // Zero base amount keeps initial crafting/discovery independent of later materials.
                var requirement = new Piece.Requirement { m_resItem = item, m_amount = 0, m_amountPerLevel = 0, m_recover = false };
                resources.Add(requirement);
                Owned.Add(requirement, item.name);
            }
            recipe.m_resources = resources.ToArray();
            return true;
        }

        internal static void Restore()
        {
            foreach (var entry in Originals) if (entry.Key) entry.Key.m_resources = entry.Value;
            Originals.Clear(); Owned.Clear();
        }

        [HarmonyPatch(typeof(Piece.Requirement), "GetAmount")]
        private static class AmountPatch
        {
            private static void Postfix(Piece.Requirement __instance, int __0, ref int __result)
            {
                string item;
                // Reference-scoped: requirements belonging to other recipes are untouched.
                if (Owned.TryGetValue(__instance, out item)) __result = WispRules.MaterialCost(item, __0);
            }
        }
    }
}
