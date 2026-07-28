using RimTelligence.Source.AIStoryteller;
using Verse;

namespace RimTelligence.Source
{
    public class Settings : ModSettings
    {

        public int selectedPresetIndex = 0;

        public StorytellerPreset SelectedPreset => Presets.All[selectedPresetIndex];

        public string customStoryPrompt = "Change inside mod setting for custom prompt";

        public string grokApiKey = "";

        public bool ragEnabled = true;
        public int ragMaxResultsPerStore = 5;
        public float ragMinSimilarity = 0.5f;

        public override void ExposeData()
        {
            Scribe_Values.Look(ref customStoryPrompt, "storyPrompt", "");
            Scribe_Values.Look(ref selectedPresetIndex, "selectedPresetIndex", 0);
            Scribe_Values.Look(ref grokApiKey, "grokApiKey", "");
            Scribe_Values.Look(ref ragEnabled, "ragEnabled", true);
            Scribe_Values.Look(ref ragMaxResultsPerStore, "ragMaxResultsPerStore", 5);
            Scribe_Values.Look(ref ragMinSimilarity, "ragMinSimilarity", 0.5f);
            base.ExposeData();
        }
    }
}