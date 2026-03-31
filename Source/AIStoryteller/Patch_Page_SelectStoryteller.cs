using HarmonyLib;
using RimTelligence.Source.AIStoryteller;
using RimWorld;
using System.Collections.Generic;
using UnityEngine;
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
            Log.Message("[FirstMod] Harmony patches applied.");
        }
    }

    [HarmonyPatch(typeof(StorytellerUI), "DrawStorytellerSelectionInterface")]
    public static class Patch_DrawStorytellerSelectionInterface
    {
        // Track dropdown open state
        private static bool dropdownOpen = false;
        private static Vector2 promptScrollPosition = Vector2.zero;

        public static void Postfix(Rect rect, ref StorytellerDef chosenStoryteller,
            ref DifficultyDef difficulty, Listing_Standard infoListing)
        {
            if (chosenStoryteller?.defName != "AI_AIStoryteller")
            {
                dropdownOpen = false;
                return;
            }

            List<StorytellerPreset> presets = Presets.All;
            StorytellerPreset current = AIStorytellerMod.settings.SelectedPreset;

            float rightPanelWidth = 320f;
            float rightPanelX = rect.width - rightPanelWidth - 20f;

            Rect rightPanelRect = new Rect(rect.x + rightPanelX, rect.y, rightPanelWidth, rect.height);

            Listing_Standard rightListing = new Listing_Standard();
            rightListing.Begin(rightPanelRect);

            rightListing.Label("Prompt Preview:", -1f);
            rightListing.Gap(4f);

            string promptText = current.Prompt;

            float promptHeight = Mathf.Min(200f, Text.CalcHeight(promptText, rightPanelWidth - 16f));
            Rect promptRect = rightListing.GetRect(promptHeight);

            GUI.BeginGroup(promptRect);

            Widgets.DrawBoxSolid(new Rect(0, 0, promptRect.width, promptRect.height), new Color(0.1f, 0.1f, 0.1f, 0.8f));

            Rect textViewRect = new Rect(4, 4, promptRect.width - 8f, promptRect.height - 8f);

            if (Text.CalcHeight(promptText, textViewRect.width) > textViewRect.height)
            {
                Rect scrollRect = new Rect(0, 0, promptRect.width, promptRect.height);
                Rect viewRect = new Rect(0, 0, promptRect.width - 20f, Text.CalcHeight(promptText, textViewRect.width));

                Widgets.BeginScrollView(scrollRect, ref promptScrollPosition, viewRect);
                Widgets.Label(new Rect(4, 4, viewRect.width, viewRect.height), promptText);
                Widgets.EndScrollView();
            }
            else
            {
                Widgets.Label(new Rect(4, 4, textViewRect.width, textViewRect.height), promptText);
            }

            GUI.EndGroup();

            rightListing.Gap(12f);

            rightListing.Label("Storyteller Preset:", -1f);
            rightListing.Gap(4f);

            Rect dropdownRect = rightListing.GetRect(30f);

            Rect buttonRect = new Rect(dropdownRect.x, dropdownRect.y, dropdownRect.width, dropdownRect.height);
            string buttonText = current.Label + (dropdownOpen ? " ▲" : " ▼");

            if (Widgets.ButtonText(buttonRect, buttonText))
            {
                dropdownOpen = !dropdownOpen;
            }

            if (dropdownOpen)
            {
                float itemHeight = 28f;
                float menuWidth = rightPanelWidth;
                float menuHeight = itemHeight * presets.Count + 8f;

                Rect menuRect = new Rect(
                    buttonRect.x,
                    buttonRect.y + buttonRect.height + 2f,
                    menuWidth,
                    menuHeight
                );

                // Set depth to render on top of everything
                GUI.depth = -100;

                // Draw menu background
                Widgets.DrawBoxSolid(menuRect, new Color(0.15f, 0.15f, 0.15f, 0.97f));
                Widgets.DrawBox(menuRect);

                // Draw menu items
                float startY = menuRect.y + 4f;
                for (int i = 0; i < presets.Count; i++)
                {
                    StorytellerPreset preset = presets[i];
                    Rect itemRect = new Rect(
                        menuRect.x + 4f,
                        startY + i * itemHeight,
                        menuWidth - 8f,
                        itemHeight
                    );

                    // Highlight hovered row
                    if (Mouse.IsOver(itemRect))
                        Widgets.DrawHighlight(itemRect);

                    // Highlight selected row
                    if (i == AIStorytellerMod.settings.selectedPresetIndex)
                        Widgets.DrawHighlightSelected(itemRect);

                    Rect labelRect = new Rect(
                        itemRect.x + 8f,
                        itemRect.y + (itemHeight - Text.LineHeight) / 2,
                        itemRect.width - 16f,
                        Text.LineHeight
                    );
                    Widgets.Label(labelRect, preset.Label);

                    if (Widgets.ButtonInvisible(itemRect))
                    {
                        AIStorytellerMod.settings.selectedPresetIndex = i;
                        AIStorytellerMod.settings.Write();
                        dropdownOpen = false;
                    }
                }

                // Reset depth
                GUI.depth = 0;

                if (Event.current.type == EventType.MouseDown &&
                    !menuRect.Contains(Event.current.mousePosition) &&
                    !buttonRect.Contains(Event.current.mousePosition))
                {
                    dropdownOpen = false;
                    Event.current.Use();
                }
            }

            rightListing.End();
        }
    }
}