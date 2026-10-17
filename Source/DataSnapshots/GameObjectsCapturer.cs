using RimTelligence.AIStoryteller;
using RimTelligence.CustomEvents.Execution;
using RimTelligence.Memory;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Verse;

namespace RimTelligence.DataSnapshots
{
    public class GameObjectsCapturer
    {
        private static IEmbeddingProvider _sharedEmbeddingProvider;
        private static readonly object _providerLock = new object();
        private static volatile bool _populationInProgress;

        /// <summary>
        /// Lazily-created shared provider (BuiltIn ONNX). Created on demand so
        /// game startup is not blocked by ONNX model load.
        /// Prefer <see cref="ResolveEmbeddingProvider"/> for population/query paths
        /// that must respect the configured embedding source.
        /// </summary>
        public static IEmbeddingProvider EmbeddingProvider
        {
            get
            {
                if (_sharedEmbeddingProvider == null)
                {
                    lock (_providerLock)
                    {
                        if (_sharedEmbeddingProvider == null)
                            _sharedEmbeddingProvider = new OnnxEmbeddingProvider();
                    }
                }
                return _sharedEmbeddingProvider;
            }
        }

        public static IEmbeddingProvider ResolveEmbeddingProvider()
        {
            Settings settings = AIStorytellerMod.Settings;
            if (settings != null && settings.embeddingSource == "Provider")
                return new LlmEmbeddingProvider();

            return new OnnxEmbeddingProvider();
        }

        /// <summary>
        /// Called once on game startup (from <see cref="RagBootstrap"/>).
        /// Populates the static vector store in the background when it is empty.
        /// Safe to call when no game is loaded: DB writes happen directly on the
        /// worker thread, in-game letters are only sent when a game is running.
        /// </summary>
        public static void EnsurePopulatedOnStartup()
        {
            try
            {
                if (string.IsNullOrEmpty(RagBootstrap.DbPath))
                {
                    ModLogger.Warning("RAG startup population skipped: DbPath not set.");
                    return;
                }

                var store = new StaticRagStore(RagBootstrap.DbPath);
                if (!store.IsEmpty())
                {
                    ModLogger.Message("Static RAG store already populated (" + store.Count() + " chunks), skipping startup population.");
                    return;
                }

                ModLogger.Message("Static RAG store is empty, populating on startup...");
                PopulateVectorStore();
            }
            catch (Exception ex)
            {
                ModLogger.Error("RAG startup population check failed: " + ex.Message);
            }
        }

        public static void PopulateVectorStore()
        {
            if (_populationInProgress)
            {
                ModLogger.Message("RAG population already in progress, skipping duplicate request.");
                return;
            }
            _populationInProgress = true;
            Task.Run(() => PopulateVectorStoreCoreAsync());
        }

        private static async Task PopulateVectorStoreCoreAsync()
        {
            try
            {
                Settings settings = AIStorytellerMod.Settings;

                if (settings != null && settings.embeddingSource == "Provider")
                {
                    string validationError = LlmEmbeddingProvider.ValidateConfig(settings);
                    if (validationError != null)
                    {
                        ModLogger.Error("RAG population aborted: " + validationError);
                        NotifyInGame(
                            "RAG Embeddings Not Generated",
                            "The provider-based embedding source is not configured.\n" + validationError,
                            LetterDefOf.NegativeEvent);
                        return;
                    }
                }

                // Touching the registry triggers its static init (defs must be loaded;
                // RagBootstrap runs in StaticConstructorOnStartup phase, after defs).
                if (!ItemCategoryRegistry.IsLoaded || ItemCategoryRegistry.AllCategories.Count == 0)
                {
                    ModLogger.Warning("RAG population aborted: item categories are not loaded yet.");
                    return;
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
                    NotifyInGame(
                        "RAG Embeddings Not Generated",
                        "Embedding generation failed:\n" + exception.Message,
                        LetterDefOf.NegativeEvent);
                    return;
                }

                if (chunks.Count == 0)
                {
                    ModLogger.Warning("RAG population produced no chunks, store left untouched.");
                    return;
                }

                // Skip re-population if another run filled the store meanwhile.
                try
                {
                    var existing = new StaticRagStore(RagBootstrap.DbPath);
                    if (!existing.IsEmpty() && chunks.Count <= existing.Count())
                    {
                        ModLogger.Message("Static vector store was populated concurrently, skipping write.");
                        return;
                    }
                }
                catch (Exception ex)
                {
                    ModLogger.Warning("RAG pre-write check failed, proceeding anyway: " + ex.Message);
                }

                // Write directly on the worker thread: no Game/main-thread
                // dependency, so this also works at the main menu on startup.
                try
                {
                    var store = new StaticRagStore(RagBootstrap.DbPath);
                    store.ClearAll();
                    foreach (VectorChunk chunk in chunks)
                        store.AddChunk(chunk.Category, chunk.DefName, chunk.Text, chunk.Embedding);

                    ModLogger.Message("Static vector store populated from config-driven categories (" + chunks.Count + " chunks).");
                }
                catch (Exception exception)
                {
                    ModLogger.Error("RAG store write failed: " + exception.Message);
                }
            }
            finally
            {
                _populationInProgress = false;
            }
        }

        /// <summary>
        /// Sends a letter when a game is running, otherwise just logs.
        /// At startup (main menu) there is no LetterStack, so letters are skipped.
        /// </summary>
        private static void NotifyInGame(string title, string text, LetterDef letterDef)
        {
            try
            {
                if (Current.Game == null || Find.LetterStack == null)
                    return;
                RimTelligenceDispatcher.ExecutionQueue.Enqueue(() =>
                {
                    try
                    {
                        Find.LetterStack.ReceiveLetter(title, text, letterDef);
                    }
                    catch (Exception ex)
                    {
                        ModLogger.Error("Failed to send RAG notification letter: " + ex.Message);
                    }
                });
            }
            catch (Exception ex)
            {
                ModLogger.Error("Failed to queue RAG notification: " + ex.Message);
            }
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
