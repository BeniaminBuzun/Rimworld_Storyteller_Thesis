using RimTelligence.AIStoryteller;
using RimTelligence.Llm;
using System.Collections.Generic;
using UnityEngine;
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

        private static readonly HashSet<string> secretFieldsEditing = new HashSet<string>();

        /// <summary>Call when the settings window opens/closes to re-hide all secret fields.</summary>
        public static void ResetSecretFields() => secretFieldsEditing.Clear();

        /// <summary>
        /// Draws an API key field. Shows only a status ("Not set" / "Set") with a button;
        /// clicking it reveals a masked input with Done and Clear buttons.
        /// Returns the (possibly modified) value.
        /// </summary>
        public static string DrawSecretField(Rect rect, string value, string controlName)
        {
            value = value ?? "";
            bool isEditing = secretFieldsEditing.Contains(controlName);

            const float btnW = 70f;
            const float gap = 4f;

            if (!isEditing)
            {
                Rect labelRect = new Rect(rect.x, rect.y, rect.width - btnW - gap, rect.height);
                Rect changeRect = new Rect(labelRect.xMax + gap, rect.y, btnW, rect.height);

                string status = value.Length == 0
                    ? "Not set"
                    : "Setted";

                Text.Anchor = TextAnchor.MiddleLeft;
                Widgets.Label(labelRect, status);
                Text.Anchor = TextAnchor.UpperLeft;

                if (Widgets.ButtonText(changeRect, value.Length == 0 ? "Set" : "Change"))
                {
                    secretFieldsEditing.Add(controlName);
                    GUI.FocusControl(controlName); // focus the field once it appears
                }
            }
            else
            {
                Rect fieldRect = new Rect(rect.x, rect.y, rect.width - (btnW * 2) - (gap * 2), rect.height);
                Rect doneRect = new Rect(fieldRect.xMax + gap, rect.y, btnW, rect.height);
                Rect clearRect = new Rect(doneRect.xMax + gap, rect.y, btnW, rect.height);

                GUI.SetNextControlName(controlName);
                value = Widgets.TextField(fieldRect, value); // plain text, not masked

                if (Widgets.ButtonText(doneRect, "Done"))
                {
                    secretFieldsEditing.Remove(controlName);
                    GUI.FocusControl(null);
                }
                if (Widgets.ButtonText(clearRect, "Clear"))
                {
                    value = "";
                }
            }

            return value;
        }

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