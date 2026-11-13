using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using System;

namespace OctoberJemini.CompactCraftingUpgrades
{
    [BepInPlugin(Id, "Compact Crafting Upgrades", "1.0.0")]
    [BepInIncompatibility("DeathWizsh.CraftingStationTweakz")]
    public sealed class Plugin : BaseUnityPlugin
    {
        public const string Id = "OctoberJemini.CompactCraftingUpgrades";
        internal static ConfigEntry<bool> Enabled;
        internal static ConfigEntry<float> Spacing;
        private Harmony harmony;

        private void Awake()
        {
            Enabled = Config.Bind("General", "Enabled", true,
                "Use custom spacing for crafting station upgrades. False restores the game's spacing check.");
            Spacing = Config.Bind("General", "MinimumUpgradeSpacing", 0.5f,
                new ConfigDescription("Minimum distance in meters between station upgrades. 0 removes this spacing check. Does not bypass collisions, station connection range, or other building rules.",
                    new AcceptableValueRange<float>(0f, 2f)));
            harmony = new Harmony(Id);
            harmony.PatchAll(typeof(Plugin).Assembly);
            Logger.LogInfo("Upgrade spacing: " + Spacing.Value + " m. Station ranges and bonuses are unchanged.");
        }

        private void OnDestroy()
        {
            if (harmony != null) harmony.UnpatchSelf();
        }
    }

    [HarmonyPatch(typeof(StationExtension), "OtherExtensionInRange", new Type[] { typeof(float) })]
    internal static class UpgradeSpacingPatch
    {
        private static void Prefix(ref float __0)
        {
            if (Plugin.Enabled.Value)
            {
                float spacing = Plugin.Spacing.Value;
                // Retain vanilla behavior if a hand-edited config supplies a non-finite value.
                if (!float.IsNaN(spacing) && !float.IsInfinity(spacing))
                    __0 = Math.Max(0f, Math.Min(2f, spacing));
            }
        }
    }
}
