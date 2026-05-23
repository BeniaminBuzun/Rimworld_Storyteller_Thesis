using System.Collections.Generic;

namespace RimTelligence.Source.AIStoryteller
{
    internal class Presets
    {
        public static List<StorytellerPreset> All
        {
            get
            {
                var presets = new List<StorytellerPreset>
                {
                    new StorytellerPreset("Balanced", "You are a neutral RimWorld storyteller. Balance threats and rewards evenly. Vary event types and pacing naturally."),
                    new StorytellerPreset("Hostile World", "You are an aggressive RimWorld storyteller. Favor raids, harsh weather, disease, and resource scarcity. Show no mercy."),
                    new StorytellerPreset("Peaceful Run", "You are a gentle RimWorld storyteller. Minimize raids and disasters. Favor trade caravans, good weather, and positive events."),
                    new StorytellerPreset("Dramatic", "You are a dramatic RimWorld storyteller. Create emotional highs and lows. Let the colony thrive just before striking hard. Tell a story worth remembering."),
                    new StorytellerPreset("Chaos", "You are an unpredictable RimWorld storyteller. Fire random events constantly, mix positive and negative wildly. Nothing should feel safe or expected.")
                };

                if (AIStorytellerMod.Settings != null)
                    presets.Add(new StorytellerPreset("User Prompt", AIStorytellerMod.Settings.customStoryPrompt));

                return presets;
            }
        }
    }

    public class StorytellerPreset
    {
        public string Label;
        public string Prompt;

        public StorytellerPreset(string label, string prompt)
        {
            Label = label;
            Prompt = prompt;
        }
    }
}