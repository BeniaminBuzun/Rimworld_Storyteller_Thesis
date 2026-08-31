using RimTelligence.AIStoryteller;
using RimTelligence.CustomEvents;
using RimTelligence.CustomEvents.Execution;
using RimTelligence.DataSnapshots;
using RimTelligence.Memory;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Verse;

namespace RimTelligence.DataSnapshots
{
    [StaticConstructorOnStartup]
    public class GameObjectsCapturer
    {
        public static readonly IEmbeddingProvider EmbeddingProvider = new OnnxEmbeddingProvider();

        public static IEmbeddingProvider ResolveEmbeddingProvider()
        {
            Settings settings = AIStorytellerMod.Settings;
            if (settings != null && settings.embeddingSource == "Provider")
                return new LlmEmbeddingProvider();

            return new OnnxEmbeddingProvider();
        }

        public static void PopulateVectorStore()
        {
            Task.Run(() => PopulateVectorStoreCoreAsync());
        }

        private static async Task PopulateVectorStoreCoreAsync()
        {
            Settings settings = AIStorytellerMod.Settings;

            if (settings != null && settings.embeddingSource == "Provider")
            {
                string validationError = LlmEmbeddingProvider.ValidateConfig(settings);
                if (validationError != null)
                {
                    ModLogger.Error("RAG population aborted: " + validationError);
                    RimTelligenceDispatcher.ExecutionQueue.Enqueue(() =>
                    {
                        Find.LetterStack.ReceiveLetter(
                            "RAG Embeddings Not Generated",
                            "The provider-based embedding source is not configured.\n" + validationError,
                            LetterDefOf.NegativeEvent);
                    });
                    return;
                }
            }

            var chunks = new List<VectorChunk>();

            try
            {
                IEmbeddingProvider provider = ResolveEmbeddingProvider();
                using (provider as IDisposable)
                {
                    foreach (var categoryEntry in ItemCategoryRegistry.AllCategories)
                    {
                        string category = categoryEntry.Key;
                        var items = categoryEntry.Value.Items;

                        foreach (var item in items)
                        {
                            string text = string.IsNullOrEmpty(item.Description)
                                ? item.Label
                                : item.Label + ": " + item.Description;

                            float[] embedding = await provider.EmbedAsync(text);
                            chunks.Add(new VectorChunk { Category = category, DefName = item.DefName, Text = text, Embedding = embedding });
                        }
                    }
                }
            }
            catch (Exception exception)
            {
                ModLogger.Error("RAG population failed: " + exception.Message);
                RimTelligenceDispatcher.ExecutionQueue.Enqueue(() =>
                {
                    Find.LetterStack.ReceiveLetter(
                        "RAG Embeddings Not Generated",
                        "Embedding generation failed:\n" + exception.Message,
                        LetterDefOf.NegativeEvent);
                });
                return;
            }

            RimTelligenceDispatcher.ExecutionQueue.Enqueue(() =>
            {
                try
                {
                    var store = new StaticRagStore(RagBootstrap.DbPath);
                    store.ClearAll();
                    foreach (VectorChunk chunk in chunks)
                        store.AddChunk(chunk.Category, chunk.DefName, chunk.Text, chunk.Embedding);

                    ModLogger.Message("Static vector store populated from config-driven categories.");
                }
                catch (Exception exception)
                {
                    ModLogger.Error("RAG store write failed: " + exception.Message);
                }
            });
        }

        private class VectorChunk
        {
            public string Category;
            public string DefName;
            public string Text;
            public float[] Embedding;
        }
    }
}
