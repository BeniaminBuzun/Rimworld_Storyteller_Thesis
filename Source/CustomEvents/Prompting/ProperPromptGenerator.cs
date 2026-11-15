using RimTelligence.AIStoryteller;
using RimTelligence.CustomEvents.Handlers;
using RimTelligence.DataSnapshots;
using RimTelligence.Memory;
using RimTelligence.Source.Memory.Embedding;
using RimTelligence.Source.Memory.Search;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RimTelligence.CustomEvents.Prompting
{
    /// <summary>
    /// Builds the storyteller prompt: the cued consequences, only the snapshot facts those consequences can use,
    /// a few writing rules, and the output format. Replaces SnapshotNarrator.
    /// The letter is the CAUSE; the effects are the CONSEQUENCES of the story.
    /// </summary>
    public static class ProperPromptGenerator
    {
        private static readonly Random _random = new Random();

        private const int DetailedPawnsPerEffect = 2;
        private const int MaxDetailedPawns = 6;
        private const int MaxListed = 6;
        private const float NutritionPerColonistPerDay = 1.6f;

        // ----------------------------------------------------------------- tones

        private static readonly (string name, string voice, string sign)[] _tones =
        {
            ("Dry Chronicler", "Understated, slightly cynical. Prefer irony over drama. Facts first, feelings never.", "Record the facts. Spare the feelings."),
            ("Absurdist", "Reality misbehaves. Cause and effect are suggestions. Use one image that makes no logical sense but feels completely true.", "Make it weird. Make it true."),
            ("Anarchic Trickster", "Narrating your own prank. Gifts have hidden costs. Good news arrives in bad packaging. Use second person occasionally.", "The punchline is always suffering."),
            ("Reluctant Optimist", "You want to be hopeful. You're trying very hard. The disaster is reframed as opportunity but you don't quite believe it yourself.", "Something good is always trying to happen."),
            ("Bored Deity", "Ancient, condescending, mildly entertained. Call colonists 'mortals'. Use passive constructions as if events simply occur.", "These creatures amuse me. For now."),
            ("Ufologist", "Everything connects to something larger. Mundane events have sinister origins. One unexplained technical detail per event.", "They were always here."),
            ("Field Medic", "Clipped, clinical, matter-of-fact. You've seen worse. Emotions are diagnoses.", "Triage everything. Mourn nothing."),
            ("Tired Poet", "Lyrical but exhausted. Beauty in collapse. Short sentences. One image that lingers.", "Even ruin has a shape."),
        };

        public static string BuildSystemPrompt(List<IEffect> chosenEvents, ColonySnapshot snapshot)
        {
            return BuildSystemPrompt(chosenEvents, snapshot, null, null);
        }

        public static string BuildSystemPrompt(
            List<IEffect> chosenEvents,
            ColonySnapshot snapshot,
            IEmbeddingProvider embeddingProvider,
            StaticRagStore ragStore)
        {
            return BuildSystemPromptAsync(chosenEvents, snapshot, embeddingProvider, ragStore).GetAwaiter().GetResult();
        }

        public static Task<string> BuildSystemPromptAsync(List<IEffect> chosenEvents, ColonySnapshot snapshot)
        {
            return BuildSystemPromptAsync(chosenEvents, snapshot, null, null);
        }

        public static async Task<string> BuildSystemPromptAsync(
            List<IEffect> chosenEvents,
            ColonySnapshot snapshot,
            IEmbeddingProvider embeddingProvider,
            StaticRagStore ragStore)
        {
            chosenEvents = chosenEvents ?? new List<IEffect>();

            var tone = _tones[_random.Next(_tones.Length)];
            int sentences = Math.Min(chosenEvents.Count + 2, 6);   // 1 event = 2, 2 = 3, 3+ = 4

            var sb = new StringBuilder();

            sb.AppendLine("You are a RimWorld storyteller narrator. You are terse and distinctive.");
            sb.AppendLine($"Voice: {tone.name} - {tone.voice}");
            sb.AppendLine($"Principle: \"{tone.sign}\"");
            sb.AppendLine();

            sb.AppendLine("HOW THIS WORKS: You write a tiny story. Afterwards the game applies the CONSEQUENCES below to the colony. " +
                          "Your story is the CAUSE; the consequences are what follows from it. The reader sees the story, then sees the colony change.");
            sb.AppendLine();

            sb.AppendLine("CONSEQUENCES (apply exactly these effectName values):");

            IEmbeddingProvider provider = embeddingProvider;
            StaticRagStore store = ragStore;

            if (provider == null && store == null)
            {
                try
                {
                    if (!string.IsNullOrEmpty(RagBootstrap.DbPath) && File.Exists(RagBootstrap.DbPath))
                    {
                        store = new StaticRagStore(RagBootstrap.DbPath);
                        provider = GameObjectsCapturer.GetSharedEmbeddingProvider();
                    }
                }
                catch (Exception ex)
                {
                    ModLogger.Warning($"ProperPromptGenerator could not initialize RAG: {ex.Message}");
                    provider = null;
                    store = null;
                }
            }
            else if (store == null && !string.IsNullOrEmpty(RagBootstrap.DbPath) && File.Exists(RagBootstrap.DbPath))
            {
                try { store = new StaticRagStore(RagBootstrap.DbPath); } catch { /* ignore */ }
            }
            else if (provider == null && store != null)
            {
                try
                {
                    provider = GameObjectsCapturer.GetSharedEmbeddingProvider();
                }
                catch { /* ignore */ }
            }

            var queryCache = new Dictionary<string, float[]>(StringComparer.Ordinal);

            foreach (IEffect chosen in chosenEvents)
                await AppendEventAsync(sb, chosen, snapshot, provider, store, queryCache);

            sb.AppendLine();

            sb.AppendLine(SnapshotNarrator.Narrate(snapshot));

            sb.AppendLine("WRITING RULES:");
            sb.AppendLine("- letterTitle: 2-4 words, no punctuation.");
            sb.AppendLine($"- letterText: exactly {sentences} sentences. One small story about WHY things change: a gesture, omen, bargain, mistake, rumor or mood that explains the consequences.");
            sb.AppendLine("- NEVER describe a consequence directly. Do not name its mechanism (no pod, meteorite, blight, egg, skill points, defNames, severity words). Let the reader infer it.");
            sb.AppendLine("- Several consequences: invent ONE single cause that explains all of them together.");
            sb.AppendLine("- Severity sets the scale of the cause: LOW is a whisper, MEDIUM a quarrel, HIGH a catastrophe, EXTREME a rupture in the world.");
            sb.AppendLine("- You may invent the cause itself (an omen, a superstition, a promise) but never new colonists. Colonists come only from the facts.");
            sb.AppendLine("- A consequence with a targetPawn: the story is about ONLY that colonist. No other colonist appears, by name or implication.");
            sb.AppendLine("- A consequence without a targetPawn may involve a colonist only if they caused it.");
            sb.AppendLine("- If the pawn's current action contradicts the story, bend the action to fit.");
            AppendTwist(sb);
            sb.AppendLine();

            sb.AppendLine("EXAMPLE (different events, for shape only; do not copy):");
            sb.AppendLine("Consequences: CropBlight; MiracleHealDisease on Mara.");
            sb.AppendLine("letterText: \"Mara spent the night begging the soil to take her fever instead. By dawn the fever was gone, and the fields smelled of something spent.\"");
            sb.AppendLine("Bad: \"Mara was cured of her disease and a blight hit the crops.\" (describes effects instead of causing them)");
            sb.AppendLine();

            sb.AppendLine("OUTPUT: one JSON object, no markdown fences, no preamble:");
            sb.AppendLine("{\"letterTitle\":\"...\",\"letterText\":\"...\",\"effects\":[{\"effectName\":\"...\",\"selectedDefName\":[\"...\"],\"severity\":\"LOW|MEDIUM|HIGH|EXTREME\",\"targetPawn\":\"...\"}]}");
            sb.AppendLine("'effects' must contain exactly one object per consequence above, never empty. Include only the fields listed under that consequence.");
            return sb.ToString();
        }

        // ----------------------------------------------------------------- events

        private static async Task AppendEventAsync(
            StringBuilder sb,
            IEffect effect,
            ColonySnapshot snapshot,
            IEmbeddingProvider provider,
            StaticRagStore store,
            Dictionary<string, float[]> queryCache)
        {
            EffectConfig contract = EffectContractProvider.GetContract(effect, snapshot);

            // NarrativeDescription should be outcome-only (e.g. "the colony receives an unexpected supply"),
            // not the mechanism ("a resource pod crashes"), or small models will echo it into the story.
            sb.AppendLine($"- {contract.EffectName}: {contract.NarrativeDescription}");
            if (contract.UsesSeverity)
                sb.AppendLine("    severity: LOW|MEDIUM|HIGH|EXTREME");
            if (contract.UsesTargetPawn)
                sb.AppendLine("    targetPawn: one colonist name from the facts below");

            if (!contract.IsDefRequired)
                return;

            if (contract.DefCount > 1)
            {
                string query1 = BuildRagQuery(contract, contract.RelatedDefList, snapshot);
                string query2 = BuildRagQuery(contract, contract.SecondaryRelatedDefList, snapshot);

                string choices1 = await DefChoicesAsync(contract.AllowedDefs, query1, provider, store, queryCache);
                string choices2 = await DefChoicesAsync(contract.SecondaryAllowedDefs, query2, provider, store, queryCache);

                sb.AppendLine($"    selectedDefName: exactly {contract.DefCount} values, in this order:");
                sb.AppendLine($"      1) {SlotLabel(contract.RelatedDefList, "defName")}: {choices1}");
                sb.AppendLine($"      2) {SlotLabel(contract.SecondaryRelatedDefList, "defName")}: {choices2}");
            }
            else
            {
                string query = BuildRagQuery(contract, contract.RelatedDefList, snapshot);
                string choices = await DefChoicesAsync(contract.AllowedDefs, query, provider, store, queryCache);
                sb.AppendLine($"    selectedDefName: exactly 1 value. Choose from: {choices}");
            }
        }

        private static string SlotLabel(string listId, string fallback)
        {
            if (string.IsNullOrEmpty(listId)) return fallback;
            foreach (string key in new[] { "Damage", "BodyPart", "Skill", "Disease", "Weather", "Animal", "Plant" })
                if (listId.IndexOf(key, StringComparison.OrdinalIgnoreCase) >= 0)
                    return key == "BodyPart" ? "body part" : key == "Damage" ? "damage type" : key.ToLowerInvariant();
            return fallback;
        }

        private static string BuildRagQuery(EffectConfig contract, string slotListId, ColonySnapshot snapshot)
        {
            var sb = new StringBuilder();

            if (contract != null)
            {
                if (!string.IsNullOrEmpty(contract.NarrativeDescription))
                    sb.Append(contract.NarrativeDescription).Append(". ");
                else if (!string.IsNullOrEmpty(contract.EffectName))
                    sb.Append(contract.EffectName).Append(". ");
            }

            if (!string.IsNullOrEmpty(slotListId))
            {
                sb.Append("Category: ").Append(slotListId).Append(". ");
            }

            if (snapshot != null)
            {
                var contextParts = new List<string>();

                if (!string.IsNullOrEmpty(snapshot.biome))
                    contextParts.Add("biome: " + snapshot.biome);
                if (!string.IsNullOrEmpty(snapshot.season))
                    contextParts.Add("season: " + snapshot.season);
                if (!string.IsNullOrEmpty(snapshot.currentWeather))
                    contextParts.Add("weather: " + snapshot.currentWeather);

                contextParts.Add($"temperature: {snapshot.temperature:F0}C");

                if (snapshot.activeConditions != null && snapshot.activeConditions.Count > 0)
                    contextParts.Add("conditions: " + string.Join(", ", snapshot.activeConditions));

                if (snapshot.colonistCount > 0)
                {
                    float daysOfFood = snapshot.totalNutrition / (snapshot.colonistCount * NutritionPerColonistPerDay);
                    if (daysOfFood < 2f)
                        contextParts.Add("food shortage");
                }

                if (contextParts.Count > 0)
                {
                    sb.Append("Colony environment: ").Append(string.Join(", ", contextParts)).Append(".");
                }
            }

            return sb.ToString().Trim();
        }

        // Queries RAG asynchronously to get the most relevant items ONLY from the selected list (defs).
        // Uses ragMaxResultsPerStore and ragMinSimilarity from mod settings.
        // Backfills remaining slots from defs randomly, and falls back fully to random sampling if RAG is unavailable.
        private static async Task<string> DefChoicesAsync(
            List<GameObjectMeta> defs,
            string queryText = null,
            IEmbeddingProvider provider = null,
            StaticRagStore store = null,
            Dictionary<string, float[]> queryCache = null)
        {
            if (defs == null || defs.Count == 0) return "any valid defName";

            List<GameObjectMeta> validDefs = defs.Where(d => !string.IsNullOrEmpty(d?.DefName)).ToList();
            if (validDefs.Count == 0) return "any valid defName";

            Settings settings = AIStorytellerMod.Settings;
            int maxResults = settings != null ? settings.ragMaxResultsPerStore : MaxListed;
            float minSimilarity = settings != null ? settings.ragMinSimilarity : 0f;

            if (provider != null && store != null && !string.IsNullOrWhiteSpace(queryText))
            {
                try
                {
                    float[] queryEmbedding = null;
                    if (queryCache != null && queryCache.TryGetValue(queryText, out float[] cached))
                    {
                        queryEmbedding = cached;
                    }
                    else
                    {
                        queryEmbedding = await provider.EmbedAsync(queryText);
                        if (queryCache != null && queryEmbedding != null)
                            queryCache[queryText] = queryEmbedding;
                    }

                    if (queryEmbedding != null && queryEmbedding.Length > 0)
                    {
                        var defMap = new Dictionary<string, GameObjectMeta>(StringComparer.OrdinalIgnoreCase);
                        foreach (var d in validDefs)
                        {
                            if (!defMap.ContainsKey(d.DefName))
                                defMap[d.DefName] = d;
                        }

                        // Ask for maxResults candidates; minSimilarity is applied below as a post-filter.
                        List<RagResult> results = store.Query(queryEmbedding, topK: maxResults, defNames: defMap.Keys);

                        if (results != null && results.Count > 0)
                        {
                            var selected = new List<GameObjectMeta>();
                            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

                            foreach (var r in results)
                            {
                                // Skip results below the minimum similarity threshold.
                                if (r.Score < minSimilarity) continue;

                                if (r?.DefName != null && defMap.TryGetValue(r.DefName, out var meta) && seen.Add(meta.DefName))
                                    selected.Add(meta);
                            }

                            // Backfill remaining slots (up to maxResults) with random items not yet selected.
                            if (selected.Count < maxResults)
                            {
                                var remaining = validDefs
                                    .Where(d => !seen.Contains(d.DefName))
                                    .OrderBy(_ => _random.Next());

                                foreach (var d in remaining)
                                {
                                    if (seen.Add(d.DefName))
                                    {
                                        selected.Add(d);
                                        if (selected.Count >= maxResults)
                                            break;
                                    }
                                }
                            }

                            if (selected.Count > 0)
                                return string.Join(", ", selected.Select(def => def.ToString()));
                        }
                    }
                }
                catch (Exception ex)
                {
                    ModLogger.Warning($"RAG def ranking in ProperPromptGenerator failed, using fallback: {ex.Message}");
                }
            }

            // Fallback: random sample so repeated prompts vary; the validator accepts the whole list.
            IEnumerable<GameObjectMeta> sample = validDefs.Count > maxResults
                ? validDefs.OrderBy(_ => _random.Next()).Take(maxResults)
                : validDefs;
            return string.Join(", ", sample.Select(def => def.ToString()));
        }

        private static void AppendTwist(StringBuilder sb)
        {
            double roll = _random.NextDouble();   // 60% chance of no twist
            if (roll < 0.20) sb.AppendLine("- Optional twist: find the irony. One clause only.");
            else if (roll < 0.35) sb.AppendLine("- Optional twist: one detail that doesn't add up. Don't explain it.");
            else if (roll < 0.40) sb.AppendLine("- Optional twist: one image that earns its beauty.");
        }
    }
}
