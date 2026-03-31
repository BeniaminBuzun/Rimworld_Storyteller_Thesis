using UnityEngine;
using Verse;

namespace RimTelligence.Source.AIStoryteller
{
    public class AIStorytellerMod : Mod
    {
        public static Settings settings;

        public AIStorytellerMod(ModContentPack content) : base(content)
        {
            settings = GetSettings<Settings>();
        }

        public override void DoSettingsWindowContents(Rect inRect)
        {
            Listing_Standard listing = new Listing_Standard();
            listing.Begin(inRect);


            listing.Label("Grok API Key:");
            settings.grokApiKey = listing.TextEntry(settings.grokApiKey);

            listing.Gap(5f);
            listing.GapLine(); 
            listing.Gap(10f);


            listing.Label("Story prompt:");
            listing.Gap(6f);

            Rect textRect = listing.GetRect(160f);
            settings.customStoryPrompt = Widgets.TextArea(textRect, settings.customStoryPrompt);

            listing.Gap(10f);
            if (listing.ButtonText("Clear"))
                settings.customStoryPrompt = "";

            listing.End();
        }

        public override string SettingsCategory() => "AI Storyteller";
    }
}