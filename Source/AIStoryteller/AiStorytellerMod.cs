using UnityEngine;
using Verse;
using HarmonyLib;

using RimTelligence.CustomEvents;
using RimTelligence.CustomEvents.Execution;
using RimTelligence.DataSnapshots;
using RimTelligence.Llm;
using LlmTornado.Code;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace RimTelligence.AIStoryteller
{
    public class AIStorytellerMod : Mod
    {
        public static Settings Settings { get; private set; }
        private static Harmony harmony;
        private Vector2 mainScroll;
        private string connectionTestResult = "";

        public AIStorytellerMod(ModContentPack content) : base(content)
        {
            ModPaths.Initialize(content.RootDir);
            Settings = GetSettings<Settings>();

            harmony = new Harmony("com.rimtelligence.aistoryteller");
            harmony.PatchAll();

            ModLogger.Message("Harmony patches applied.");
        }

        public override void DoSettingsWindowContents(Rect inRect)
        {
            Rect outRect = new Rect(0f, 0f, inRect.width, inRect.height);

            Rect viewRect = new Rect(0f, 0f, outRect.width - 16f, 1000f);

            Widgets.BeginScrollView(outRect, ref mainScroll, viewRect);

            Listing_Standard listing = new Listing_Standard();
            listing.Begin(inRect);

            DrawLlmProviderSection(listing);

            listing.Gap(8f);
            listing.GapLine();
            listing.Gap(10f);

            DrawEmbeddingSourceSection(listing);

            listing.Gap(8f);
            listing.GapLine();
            listing.Gap(10f);

            listing.Label("Story prompt:");
            listing.Gap(6f);

            Rect textRect = listing.GetRect(160f);
            Settings.customStoryPrompt = Widgets.TextArea(textRect, Settings.customStoryPrompt);

            listing.Gap(10f);
            if (listing.ButtonText("Clear"))
                Settings.customStoryPrompt = "";

            listing.Gap(10f);
            listing.GapLine();
            listing.Gap(10f);
            listing.Label("RAG Context Settings:");

            Settings.ragMaxResultsPerStore = (int)listing.SliderLabeled(
                $"Max results per store: {Settings.ragMaxResultsPerStore}", Settings.ragMaxResultsPerStore, 1f, 20f);
            Settings.ragMinSimilarity = listing.SliderLabeled(
                $"Min similarity: {Settings.ragMinSimilarity:F2}", Settings.ragMinSimilarity, 0f, 1f);

            listing.End();
            Widgets.EndScrollView();
        }

        public override string SettingsCategory() => "AI Storyteller";

        private void DrawLlmProviderSection(Listing_Standard listing)
        {
            listing.Label("LLM Provider Settings:");
            listing.Gap(6f);

            if (ProviderCatalog.ChatCapableProviders == null || ProviderCatalog.ChatCapableProviders.Count == 0)
            {
                listing.Label("No providers available.");
                return;
            }

            List<string> providerNames = ProviderCatalog.ChatCapableProviders
                .Select(candidateProvider => candidateProvider.ToString())
                .ToList();

            if (!providerNames.Contains(Settings.activeProviderId))
            {
                Settings.activeProviderId = providerNames.Contains("Groq") ? "Groq" : providerNames[0];
                Settings.GetOrCreateProviderConfig(Settings.activeProviderId);
            }

            listing.Label("Active provider:");
            Rect providerRect = listing.GetRect(24f);
            if (Widgets.ButtonText(providerRect, Settings.activeProviderId))
            {
                var menu = new List<FloatMenuOption>();
                foreach (string name in providerNames)
                {
                    string nameLocal = name;
                    menu.Add(new FloatMenuOption(name, () =>
                    {
                        Settings.activeProviderId = nameLocal;
                        Settings.GetOrCreateProviderConfig(nameLocal);
                    }));
                }
                Find.WindowStack.Add(new FloatMenu(menu));
            }
            listing.Gap(6f);

            ProviderConfig config = Settings.GetOrCreateProviderConfig(Settings.activeProviderId);
            LLmProviders provider = ProviderCatalog.ResolveProvider(config.providerId);

            if (ProviderCatalog.IsSelfHosted(provider))
            {
                listing.Label("Server base URL:");
                config.baseUrl = listing.TextEntry(config.baseUrl);
                listing.Gap(4f);
                listing.Label("API key (optional for self-hosted):");
                config.apiKey = listing.TextEntry(config.apiKey);
            }
            else
            {
                listing.Label("API key:");
                config.apiKey = listing.TextEntry(config.apiKey);
            }

            listing.Gap(4f);
            DrawModelField(listing, config, provider, false);

            if (!string.IsNullOrEmpty(connectionTestResult))
            {
                listing.Gap(4f);
                listing.Label(connectionTestResult);
            }
        }

        private void DrawEmbeddingSourceSection(Listing_Standard listing)
        {
            listing.Label("Embedding Source (RAG):");
            listing.Gap(6f);

            listing.Label("Embedding source:");
            Rect sourceRect = listing.GetRect(24f);
            if (Widgets.ButtonText(sourceRect, Settings.embeddingSource))
            {
                var menu = new List<FloatMenuOption>
                {
                    new FloatMenuOption("BuiltIn", () => Settings.embeddingSource = "BuiltIn"),
                    new FloatMenuOption("Provider", () => Settings.embeddingSource = "Provider")
                };
                Find.WindowStack.Add(new FloatMenu(menu));
            }
            listing.Gap(6f);

            if (Settings.embeddingSource == "Provider"
                && ProviderCatalog.EmbeddingProviders != null
                && ProviderCatalog.EmbeddingProviders.Count > 0)
            {
                List<string> embeddingProviderNames = ProviderCatalog.EmbeddingProviders
                    .Select(candidateProvider => candidateProvider.ToString())
                    .ToList();

                if (!embeddingProviderNames.Contains(Settings.embeddingProviderId))
                {
                    Settings.embeddingProviderId = embeddingProviderNames.Contains("Voyage")
                        ? "Voyage"
                        : embeddingProviderNames[0];
                    Settings.GetOrCreateProviderConfig(Settings.embeddingProviderId);
                }

                listing.Label("Embedding provider:");
                Rect providerRect = listing.GetRect(24f);
                if (Widgets.ButtonText(providerRect, Settings.embeddingProviderId))
                {
                    var menu = new List<FloatMenuOption>();
                    foreach (string name in embeddingProviderNames)
                    {
                        string nameLocal = name;
                        menu.Add(new FloatMenuOption(name, () =>
                        {
                            Settings.embeddingProviderId = nameLocal;
                            Settings.GetOrCreateProviderConfig(nameLocal);
                        }));
                    }
                    Find.WindowStack.Add(new FloatMenu(menu));
                }
                listing.Gap(6f);

                ProviderConfig config = Settings.GetOrCreateProviderConfig(Settings.embeddingProviderId);
                listing.Label("API key:");
                config.apiKey = listing.TextEntry(config.apiKey);
                listing.Gap(4f);
                DrawModelField(listing, config, ProviderCatalog.ResolveProvider(config.providerId), true);

                string warning = Memory.LlmEmbeddingProvider.ValidateConfig(Settings);
                if (warning != null)
                {
                    listing.Gap(4f);
                    GUI.color = Color.yellow;
                    listing.Label(warning);
                    GUI.color = Color.white;
                }
            }

            listing.Gap(6f);
            if (listing.ButtonText("Generate Embeddings"))
                GameObjectsCapturer.PopulateVectorStore();
        }

        private void DrawModelField(Listing_Standard listing, ProviderConfig config, LLmProviders provider, bool embedding)
        {
            List<string> catalog = embedding
                ? ProviderCatalog.GetEmbeddingModelNames(provider)
                : ProviderCatalog.GetChatModelNames(provider);

            string currentModel = embedding ? config.embeddingModel : config.chatModel;
            string fieldLabel = embedding ? "Embedding model" : "Model";

            if (catalog.Count == 0)
            {
                listing.Label(fieldLabel + " (free text):");
                listing.Gap(4f);
                string edited = listing.TextEntry(currentModel);
                if (embedding) config.embeddingModel = edited;
                else config.chatModel = edited;
                return;
            }

            listing.Label(fieldLabel + ":");
            listing.Gap(4f);

            Rect presetRect = listing.GetRect(24f);
            string presetLabel = catalog.Contains(currentModel) ? currentModel : "Custom model...";
            if (Widgets.ButtonText(presetRect, "Preset: " + presetLabel))
            {
                var menu = new List<FloatMenuOption>();
                foreach (string modelName in catalog)
                {
                    string modelNameLocal = modelName;
                    menu.Add(new FloatMenuOption(modelName, () =>
                    {
                        if (embedding) config.embeddingModel = modelNameLocal;
                        else config.chatModel = modelNameLocal;
                    }));
                }
                Find.WindowStack.Add(new FloatMenu(menu));
            }
            listing.Gap(4f);

            listing.Label("Free text (override):");
            string freeText = listing.TextEntry(currentModel);
            if (embedding) config.embeddingModel = freeText;
            else config.chatModel = freeText;
        }
    }
}
