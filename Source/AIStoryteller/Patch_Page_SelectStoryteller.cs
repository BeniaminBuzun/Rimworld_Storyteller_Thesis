using HarmonyLib;
using RimTelligence.Source.AIStoryteller;
using RimWorld;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using Verse;

namespace RimTelligence.Source.Storyteller
{
    [StaticConstructorOnStartup]
    public static class AIStorytellerPatchLoader
    {
        static AIStorytellerPatchLoader()
        {
            var harmony = new Harmony("com.rimtelligence.aistoryteller");
            harmony.PatchAll();
            Log.Message("[RimTelligence] Harmony patches applied.");
        }
    }

    [HarmonyPatch(typeof(StorytellerUI), "DrawStorytellerSelectionInterface")]
    public static class Patch_DrawStorytellerSelectionInterface
    {
        static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            var codes = instructions.ToList();

            var targetMethod = AccessTools.PropertyGetter(typeof(ModsConfig), nameof(ModsConfig.AnomalyActive));
            var injectionMethod = AccessTools.Method(typeof(AIStorytellerSelectInjection), nameof(AIStorytellerSelectInjection.DrawMyMenu));

            for (int i = 0; i < codes.Count; i++)
            {
                if (codes[i].Calls(targetMethod))
                {
                    var loadListing = new CodeInstruction(OpCodes.Ldarg, 4);

                    loadListing.labels = codes[i].labels.ToList();
                    codes[i].labels.Clear();

                    yield return loadListing;
                    yield return new CodeInstruction(OpCodes.Ldarg_1);
                    yield return new CodeInstruction(OpCodes.Ldind_Ref);
                    yield return new CodeInstruction(OpCodes.Call, injectionMethod);
                }

                yield return codes[i];
            }
        }
    }
}