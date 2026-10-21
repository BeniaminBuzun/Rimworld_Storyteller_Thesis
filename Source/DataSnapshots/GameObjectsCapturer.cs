using RimTelligence.AIStoryteller;
using RimTelligence.CustomEvents.Execution;
using RimTelligence.Memory;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Verse;

namespace RimTelligence.DataSnapshots
{
    /// <summary>
    /// Keeps the database in sync with the game defs.
    /// Phase 1 (fast): scan defs on the main thread, make the DB rows match, reload the registry.
    /// Phase 2 (slow, background): embed every row that has no embedding yet, in resumable batches.
    /// </summary>
    public class GameObjectsCapturer
    {
        private const int EmbedBatchSize = 200;

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

        private static string CurrentEmbeddingSource()
        {
            Settings settings = AIStorytellerMod.Settings;
            return settings != null && settings.embeddingSource == "Provider" ? "Provider" : "BuiltIn";
        }

        /// <summary>
        /// Called on startup (from <see cref="RagBootstrap"/>). Cheap when nothing changed:
        /// only new, removed or changed defs are touched, and only rows without embeddings are embedded.
        /// </summary>
        public static void EnsurePopulatedOnStartup() => StartSync(force: false);

        /// <summary>Forced rebuild: clears every embedding and regenerates all of them.</summary>
        public static void PopulateVectorStore() => StartSync(force: true);

        private static void StartSync(bool force)
        {
            if (_populationInProgress)
            {
                ModLogger.Message("RAG sync already in progress, skipping duplicate request.");
                return;
            }

            _populationInProgress = true;
            try
            {
                if (string.IsNullOrEmpty(RagBootstrap.DbPath))
                {
                    ModLogger.Warning("RAG sync skipped: DbPath not set.");
                    _populationInProgress = false;
                    return;
                }

                if (!ItemCategoryRegistry.IsLoaded || ItemCategoryRegistry.AllCategories.Count == 0)
                {
                    ModLogger.Warning("RAG sync aborted: item categories are not loaded.");
                    _populationInProgress = false;
                    return;
                }

                if (!GameDefScanner.DefsAvailable())
                {
                    ModLogger.Message("RAG sync skipped: game defs not available, the database is used as-is.");
                    _populationInProgress = false;
                    return;
                }

                // Defs must be read on the main thread.
                var scanned = GameDefScanner.Scan(ItemCategoryRegistry.AllCategories.Keys.ToList());

                Task.Run(() => SyncCoreAsync(scanned, force));
            }
            catch (Exception ex)
            {
                ModLogger.Error("RAG sync failed to start: " + ex.Message);
                _populationInProgress = false;
            }
        }

        private static string BuildText(GameObjectMeta meta) =>
            string.IsNullOrEmpty(meta.Description) ? meta.Label : meta.Label + ": " + meta.Description;

        private static async Task SyncCoreAsync(Dictionary<string, List<GameObjectMeta>> scanned, bool force)
        {
            try
            {
                var store = new StaticRagStore(RagBootstrap.DbPath);
                string source = CurrentEmbeddingSource();

                // ---- Phase 1: make the DB match the game defs ----
                string previousSource = store.GetEmbeddingSource();
                bool reset = force || (previousSource != null && previousSource != source);

                var desired = scanned.SelectMany(kv => kv.Value.Select(meta => new StaticChunk
                {
                    Category = kv.Key,
                    DefName = meta.DefName,
                    Text = BuildText(meta),
                    MetadataJson = DefMetadata.ToJson(meta)
                })).ToList();

                if (desired.Count == 0)
                {
                    ModLogger.Warning("RAG sync produced no defs, database left untouched.");
                    return;
                }

                var result = store.SyncDefs(desired, reset);
                store.SetEmbeddingSource(source);
                ModLogger.Message("RAG defs synced: +" + result.Added + " ~" + result.Updated + " -" + result.Removed
                                  + (reset ? " (embeddings reset)" : ""));

                ItemCategoryRegistry.Reload();

                // ---- Phase 2: embed whatever is still missing ----
                List<StaticChunk> pending = store.GetPendingEmbeddings();
                if (pending.Count == 0)
                {
                    ModLogger.Message("RAG store up to date (" + store.Count() + " rows).");
                    return;
                }

                if (source == "Provider")
                {
                    string validationError = LlmEmbeddingProvider.ValidateConfig(AIStorytellerMod.Settings);
                    if (validationError != null)
                    {
                        ModLogger.Error("RAG embedding aborted: " + validationError);
                        NotifyInGame(
                            "RAG Embeddings Not Generated",
                            "The provider-based embedding source is not configured.\n" + validationError,
                            LetterDefOf.NegativeEvent);
                        return;
                    }
                }

                ModLogger.Message("Embedding " + pending.Count + " rows...");

                try
                {
                    IEmbeddingProvider provider = ResolveEmbeddingProvider();
                    using (provider as IDisposable)
                    {
                        var batch = new List<StaticChunk>();
                        foreach (var chunk in pending)
                        {
                            chunk.Embedding = await provider.EmbedAsync(chunk.Text);
                            batch.Add(chunk);

                            // Save in batches so an interrupted run resumes instead of starting over.
                            if (batch.Count >= EmbedBatchSize)
                            {
                                store.SetEmbeddings(batch);
                                batch.Clear();
                            }
                        }

                        if (batch.Count > 0)
                            store.SetEmbeddings(batch);
                    }
                }
                catch (Exception exception)
                {
                    ModLogger.Error("RAG embedding failed (it resumes on the next start): " + exception.Message);
                    NotifyInGame(
                        "RAG Embeddings Not Generated",
                        "Embedding generation failed:\n" + exception.Message,
                        LetterDefOf.NegativeEvent);
                    return;
                }

                ModLogger.Message("RAG embedding finished (" + pending.Count + " rows).");
            }
            catch (Exception ex)
            {
                ModLogger.Error("RAG sync failed: " + ex.Message);
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
    }
}