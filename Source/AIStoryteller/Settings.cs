using RimTelligence.Source.AIStoryteller;
using Verse;

namespace RimTelligence.Source.AIStoryteller
{
    public class Settings : ModSettings
    {

        public int selectedPresetIndex = 0;

        public StorytellerPreset SelectedPreset => Presets.All[selectedPresetIndex];

        public string customStoryPrompt = "";

        public string grokApiKey = "";

        public override void ExposeData()
        {
            Scribe_Values.Look(ref customStoryPrompt, "storyPrompt", "");
            Scribe_Values.Look(ref selectedPresetIndex, "selectedPresetIndex", 0);
            Scribe_Values.Look(ref grokApiKey, "grokApiKey", "");
            base.ExposeData();
        }
    }
}