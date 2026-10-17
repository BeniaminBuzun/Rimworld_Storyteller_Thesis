using HarmonyLib;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using RimTelligence.CustomEvents.Handlers.Pawns.Debuffs;
using UnityEngine;
using Verse;

namespace RimTelligence.AIStoryteller
{
    [HarmonyPatch(typeof(StorytellerUI), "DrawStorytellerSelectionInterface")]
    public static class Patch_DrawStorytellerSelectionInterface
    {
        static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            var codes = instructions.ToList();

            var targetMethod = AccessTools.PropertyGetter(typeof(ModsConfig), nameof(ModsConfig.AnomalyActive));
            var injectionMethod = AccessTools.Method(typeof(AIStorytellerSelectInjection), nameof(AIStorytellerSelectInjection.DrawMyMenu));

            for (int instructionIndex = 0; instructionIndex < codes.Count; instructionIndex++)
            {
                if (codes[instructionIndex].Calls(targetMethod))
                {
                    var loadListing = new CodeInstruction(OpCodes.Ldarg, 4);

                    loadListing.labels = codes[instructionIndex].labels.ToList();
                    codes[instructionIndex].labels.Clear();

                    yield return loadListing;
                    yield return new CodeInstruction(OpCodes.Ldarg_1);
                    yield return new CodeInstruction(OpCodes.Ldind_Ref);
                    yield return new CodeInstruction(OpCodes.Call, injectionMethod);
                }

                yield return codes[instructionIndex];
            }
        }
    }

    public static class AIStorytellerSelectInjection
    {
        private static Vector2 scrollPos;

        public static void DrawMyMenu(Listing_Standard listing, StorytellerDef storyteller)
        {
            if (storyteller?.defName != "AI_AIStoryteller") return;

            listing.Gap(25f);

            listing.Label("AI Storyteller Configuration");
            listing.GapLine(6f);

            var current = AIStorytellerMod.Settings.SelectedPreset;
            if (listing.ButtonTextLabeled("Storyteller Preset:", current.Label + " ▼"))
            {
                List<FloatMenuOption> options = new List<FloatMenuOption>();

                int idx = 0;
                foreach (var preset in Presets.All)
                {
                    int capture = idx;
                    options.Add(new FloatMenuOption(preset.Label, () =>
                    {
                        AIStorytellerMod.Settings.selectedPresetIndex = capture;
                        AIStorytellerMod.Settings.Write();
                    }));
                    idx++;
                }

                Find.WindowStack.Add(new FloatMenu(options));
            }

            listing.Gap(10f);

            listing.Label("Prompt Preview:");
            string prompt = current.Prompt;
            float boxHeight = 120f;

            Rect rect = listing.GetRect(boxHeight);

            Widgets.DrawBoxSolid(rect, new Color(0f, 0f, 0f, 0.2f));
            Widgets.DrawBox(rect);

            Rect viewRect = new Rect(0, 0, rect.width - 16f, Text.CalcHeight(prompt, rect.width - 16f));
            Widgets.BeginScrollView(rect.ContractedBy(2f), ref scrollPos, viewRect);
            Widgets.Label(viewRect, prompt.Colorize(new Color(0.8f, 0.8f, 0.8f)));
            Widgets.EndScrollView();
        }
    }
}