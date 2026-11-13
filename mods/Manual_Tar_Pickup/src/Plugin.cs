using System;
using System.Collections.Generic;
using System.Reflection.Emit;
using BepInEx;
using HarmonyLib;

namespace OctoberJemini.ManualTarPickup
{
    [BepInPlugin("OctoberJemini.ManualTarPickup", "Manual Tar Pickup", "1.0.0")]
    public sealed class Plugin : BaseUnityPlugin
    {
        private Harmony harmony;

        private void Awake()
        {
            harmony = new Harmony("OctoberJemini.ManualTarPickup");
            harmony.PatchAll(typeof(Plugin).Assembly);
            Logger.LogInfo("Manual tar pickup enabled. Use your interact key to retrieve items from tar.");
        }

        private void OnDestroy()
        {
            harmony?.UnpatchSelf();
        }

        [HarmonyPatch(typeof(ItemDrop), "Interact")]
        private static class ManualInteractionPatch
        {
            private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
            {
                var result = new List<CodeInstruction>(instructions);
                var tarCheck = AccessTools.Method(typeof(ItemDrop), "InTar");
                int replacements = 0;

                for (int index = 0; index < result.Count; index++)
                {
                    if (!result[index].Calls(tarCheck))
                        continue;

                    // Consume the ItemDrop argument and supply false only inside manual interaction.
                    // Keep labels and exception markers attached to the original instruction.
                    result[index].opcode = OpCodes.Pop;
                    result[index].operand = null;
                    result.Insert(index + 1, new CodeInstruction(OpCodes.Ldc_I4_0));
                    index++;
                    replacements++;
                }

                if (replacements != 1)
                    throw new InvalidOperationException("Manual Tar Pickup expected one tar check in ItemDrop.Interact, found " + replacements);

                return result;
            }
        }
    }
}
