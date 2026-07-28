using UnityEngine;
using Verse;
using HarmonyLib;

using RimTelligence.Source.CustomEvents;
using RimTelligence.Source.DataSnapshots;

namespace RimTelligence.Source.AIStoryteller
{
    public class AIStorytellerMod : Mod
    {
        public static Settings Settings { get; private set; }
        private static Harmony harmony;

        public AIStorytellerMod(ModContentPack content) : base(content)
        {
            Settings = GetSettings<Settings>();

            harmony = new Harmony("com.rimtelligence.aistoryteller");
            harmony.PatchAll();

            ModLogger.Message("Harmony patches applied.");
        }

        public override void DoSettingsWindowContents(Rect inRect)
        {
            Listing_Standard listing = new Listing_Standard();
            listing.Begin(inRect);


            listing.Label("Grok API Key:");
            Settings.grokApiKey = listing.TextEntry(Settings.grokApiKey);

            listing.Gap(5f);
            listing.GapLine(); 
            listing.Gap(10f);


            listing.Label("Story prompt:");
            listing.Gap(6f);

            Rect textRect = listing.GetRect(160f);
            Settings.customStoryPrompt = Widgets.TextArea(textRect, Settings.customStoryPrompt);

            listing.Gap(10f);
            if (listing.ButtonText("Clear"))
                Settings.customStoryPrompt = "";

            if (listing.ButtonText("Generate Embeddings"))
                GameObjectsCapturer.PopulateVectorStore();

            listing.Gap(10f);
            listing.GapLine();
            listing.Gap(10f);
            listing.Label("RAG Context Settings:");

            listing.CheckboxLabeled("Enable RAG context", ref Settings.ragEnabled);
            Settings.ragMaxResultsPerStore = (int)listing.SliderLabeled(
                $"Max results per store: {Settings.ragMaxResultsPerStore}", Settings.ragMaxResultsPerStore, 1f, 20f);
            Settings.ragMinSimilarity = listing.SliderLabeled(
                $"Min similarity: {Settings.ragMinSimilarity:F2}", Settings.ragMinSimilarity, 0f, 1f);

            listing.End();
        }

        public override string SettingsCategory() => "AI Storyteller";
    }
}