using RimWorld;
using System.Collections.Generic;
using UnityEngine;
using Verse;

namespace RimTelligence.Source.AIStoryteller
{
    public static class AIStorytellerSelectInjection
    {
        private static Vector2 scrollPos;

        public static void DrawMyMenu(Listing_Standard listing, StorytellerDef storyteller)
        {
            if (storyteller?.defName != "AI_AIStoryteller") return;

            listing.Gap(25f);

            listing.Label("AI Storyteller Configuration");
            listing.GapLine(6f);

            var current = AIStorytellerMod.settings.SelectedPreset;
            if (listing.ButtonTextLabeled("Storyteller Preset:", current.Label + " ▼"))
            {
                List<FloatMenuOption> options = new List<FloatMenuOption>();

                int idx = 0;
                foreach (var preset in Presets.All)
                {
                    int capture = idx;
                    options.Add(new FloatMenuOption(preset.Label, () =>
                    {
                        AIStorytellerMod.settings.selectedPresetIndex = capture;
                        AIStorytellerMod.settings.Write();
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
