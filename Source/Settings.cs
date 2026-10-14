using RimTelligence.AIStoryteller;
using RimTelligence.Llm;
using System.Collections.Generic;
using Verse;

namespace RimTelligence
{
    public class Settings : ModSettings
    {

        public int selectedPresetIndex = 0;

        public StorytellerPreset SelectedPreset => Presets.All[selectedPresetIndex];

        public string customStoryPrompt = "Change inside mod setting for custom prompt";

        public string llmProviderApiKey = "";

        public int ragMaxResultsPerStore = 5;
        public float ragMinSimilarity = 0.5f;
        public bool ragEnabled = true;

        public List<ProviderConfig> providerConfigs = new List<ProviderConfig>();
        public string activeProviderId = "";
        public string embeddingSource = "BuiltIn";
        public string embeddingProviderId = "";

        public ProviderConfig GetProviderConfig(string providerId)
        {
            if (string.IsNullOrEmpty(providerId))
                return null;

            // Ensure the list wasn't set to null by Scribe
            if (providerConfigs == null)
                providerConfigs = new List<ProviderConfig>();

            // Add 'config != null' to safely skip corrupted XML entries
            return providerConfigs.Find(config => config != null && config.providerId == providerId);
        }

        public ProviderConfig GetOrCreateProviderConfig(string providerId)
        {
            ProviderConfig existing = GetProviderConfig(providerId);
            if (existing != null)
                return existing;

            existing = new ProviderConfig { providerId = providerId };
            providerConfigs.Add(existing);
            return existing;
        }

        public ProviderConfig ActiveProvider => GetProviderConfig(activeProviderId);

        public override void ExposeData()
        {
            Scribe_Values.Look(ref customStoryPrompt, "storyPrompt", "");
            Scribe_Values.Look(ref selectedPresetIndex, "selectedPresetIndex", 0);
            Scribe_Values.Look(ref llmProviderApiKey, "llmProviderApiKey", "");
            Scribe_Values.Look(ref ragEnabled, "ragEnabled", true);
            Scribe_Values.Look(ref ragMaxResultsPerStore, "ragMaxResultsPerStore", 5);
            Scribe_Values.Look(ref ragMinSimilarity, "ragMinSimilarity", 0.5f);

            Scribe_Collections.Look(ref providerConfigs, "providerConfigs", LookMode.Deep);
            Scribe_Values.Look(ref activeProviderId, "activeProviderId", "");
            Scribe_Values.Look(ref embeddingSource, "embeddingSource", "BuiltIn");
            Scribe_Values.Look(ref embeddingProviderId, "embeddingProviderId", "");

            base.ExposeData();
        }
    }
}
