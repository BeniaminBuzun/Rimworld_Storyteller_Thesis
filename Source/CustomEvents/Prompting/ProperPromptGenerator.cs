using RimTelligence.CustomEvents.Handlers;
using RimTelligence.DataSnapshots;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;

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

        // ----------------------------------------------------------------- effect name constants

        private const string EffectAnimalInsanity = "AnimalInsanity";
        private const string EffectSelfTame = "SelfTame";
        private const string EffectSpawnAnimals = "SpawnAnimals";
        private const string EffectCropBlight = "CropBlight";
        private const string EffectResourcePodCrash = "ResourcePodCrash";
        private const string EffectPawnFoundItems = "PawnFoundItems";
        private const string EffectLoseItems = "LoseItems";
        private const string EffectMiracleHealChronicCondition = "MiracleHealChronicCondition";
        private const string EffectMiracleHealMissingPart = "MiracleHealMissingPart";
        private const string EffectMiracleHealDisease = "MiracleHealDisease";
        private const string EffectDamagePawn = "DamagePawn";
        private const string EffectAddDisease = "AddDisease";
        private const string EffectMoodBuff = "MoodBuff";
        private const string EffectMoodDebuff = "MoodDebuff";
        private const string EffectAddExpPoints = "AddExpPoints";
        private const string EffectLoseExpPoints = "LoseExpPoints";

        // ------------------------------------------- which snapshot data each effect needs

        [Flags]
        private enum Focus { None = 0, Animals = 1, Crops = 2, Items = 4 }

        // Pawn data is driven by Config.UsesTargetPawn; this table covers the rest.
        private static readonly Dictionary<string, Focus> _focus = new Dictionary<string, Focus>
        {
            [EffectAnimalInsanity] = Focus.Animals,
            [EffectSelfTame] = Focus.Animals,
            [EffectSpawnAnimals] = Focus.Animals,
            [EffectCropBlight] = Focus.Crops,
            [EffectResourcePodCrash] = Focus.Items,
            [EffectPawnFoundItems] = Focus.Items,
            [EffectLoseItems] = Focus.Items,
        };

        // Which colonists are the best candidates for a pawn effect (higher = better candidate).
        private static readonly Dictionary<string, Func<PawnSnapshot, float>> _pawnScore =
            new Dictionary<string, Func<PawnSnapshot, float>>
            {
                [EffectMiracleHealChronicCondition] = p => p.health?.chronic_conditions?.Count ?? 0,
                [EffectMiracleHealMissingPart] = p => p.health?.missing_body_parts?.Count ?? 0,
                [EffectMiracleHealDisease] = p => p.health?.diseases?.Count ?? 0,
                [EffectDamagePawn] = p => p.health?.overall_health ?? 0f,
                [EffectAddDisease] = p => ((p.health?.diseases?.Count ?? 0) == 0 ? 1f : 0f) + (p.health?.overall_health ?? 0f),
                [EffectMoodBuff] = p => 1f - Mood(p),
                [EffectMoodDebuff] = p => Mood(p),
                [EffectAddExpPoints] = TopSkillLevel,
                [EffectLoseExpPoints] = TopSkillLevel,
            };

        // ----------------------------------------------------------------- entry point

        public static string BuildSystemPrompt(List<IEffect> chosenEvents, ColonySnapshot snapshot)
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
            foreach (IEffect chosen in chosenEvents)
                AppendEvent(sb, chosen, snapshot);
            sb.AppendLine();

            AppendFacts(sb, chosenEvents, snapshot);

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

        private static void AppendEvent(StringBuilder sb, IEffect effect, ColonySnapshot snapshot)
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
                sb.AppendLine($"    selectedDefName: exactly {contract.DefCount} values, in this order:");
                sb.AppendLine($"      1) {SlotLabel(contract.RelatedDefList, "defName")}: {DefChoices(contract.AllowedDefs)}");
                sb.AppendLine($"      2) {SlotLabel(contract.SecondaryRelatedDefList, "defName")}: {DefChoices(contract.SecondaryAllowedDefs)}");
            }
            else
            {
                sb.AppendLine($"    selectedDefName: exactly 1 value. Choose from: {DefChoices(contract.AllowedDefs)}");
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

        // Shows a random sample so repeated prompts vary; the validator accepts the whole list.
        private static string DefChoices(List<GameObjectMeta> defs)
        {
            if (defs == null || defs.Count == 0) return "any valid defName";
            IEnumerable<GameObjectMeta> sample = defs.Count > MaxListed
                ? defs.OrderBy(_ => _random.Next()).Take(MaxListed)
                : defs;
            return string.Join(", ", sample.Select(def => def.ToString()));
        }

        // ----------------------------------------------------------------- facts

        private static void AppendFacts(StringBuilder sb, List<IEffect> events, ColonySnapshot s)
        {
            sb.AppendLine("FACTS (use only what serves the story; never list them):");
            if (s == null)
            {
                sb.AppendLine("Colony details are unavailable.");
                sb.AppendLine();
                return;
            }

            // Always: setting and ambient one-liners (each omitted when empty).
            sb.AppendLine($"Colony \"{s.colonyName}\": day {s.day}, {s.time}:00, {s.season}, {s.biome}. " +
                          $"Weather: {s.currentWeather}, {Math.Round(s.temperature)} C. Tech level: {s.currentTechLevel}.");

            sb.AppendLine(s.daysSinceLastRaid >= 0
                ? $"Last major threat: {s.daysSinceLastRaid} days ago."
                : "No major threat has struck yet.");

            if (s.totalNutrition > 0 && s.colonistCount > 0)
                sb.AppendLine($"Food stock: about {s.totalNutrition / (s.colonistCount * NutritionPerColonistPerDay):0.#} days for the colony.");

            if (s.activeConditions?.Count > 0)
                sb.AppendLine("Active conditions: " + string.Join(", ", s.activeConditions) + ".");

            if (s.activeQuests?.Count > 0)
                sb.AppendLine("Ongoing quests: " + string.Join(", ", s.activeQuests.Take(3)) + ".");

            if (s.factionRelations?.Count > 0)
            {
                IEnumerable<string> factions = s.factionRelations
                    .OrderByDescending(kv => Math.Abs(kv.Value)).Take(4)
                    .Select(kv => $"{kv.Key} ({kv.Value:+0;-0;0})");
                sb.AppendLine("Factions (goodwill): " + string.Join(", ", factions) + ".");
            }

            var others = new List<string>();
            if (s.prisoners?.Count > 0) others.Add(s.prisoners.Count + " prisoners");
            if (s.slaves?.Count > 0) others.Add(s.slaves.Count + " slaves");
            if (s.visitors?.Count > 0) others.Add(s.visitors.Count + " visitors");
            if (others.Count > 0)
                sb.AppendLine("Also on the map: " + string.Join(", ", others) + ".");

            // Selected by the cued consequences.
            if (events.Any(e => e.Config.UsesTargetPawn))
                AppendPawns(sb, events, s);

            Focus focus = events.Aggregate(Focus.None,
                (acc, e) => _focus.TryGetValue(e.Config.EffectName, out Focus f) ? acc | f : acc);

            if ((focus & Focus.Animals) != 0)
            {
                if (s.wildAnimals?.Count > 0)
                    sb.AppendLine("Wild animals nearby: " + string.Join(", ", s.wildAnimals.OrderByDescending(a => a.count).Take(MaxListed).Select(Counted)) + ".");
                if (s.colonyAnimalCount > 0)
                    sb.AppendLine($"Colony animals ({s.colonyAnimalCount}): " + (s.colonyAnimals?.Count > 0 ? string.Join(", ", s.colonyAnimals.Select(Label)) : "unrecorded") + ".");
            }

            if ((focus & Focus.Crops) != 0 && s.cultivatedPlants?.Count > 0)
                sb.AppendLine("Crops: " + string.Join(", ", s.cultivatedPlants.Select(Counted)) + ".");

            if ((focus & Focus.Items) != 0)
            {
                sb.AppendLine($"Wealth: {Num(s.wealthTotal)} (items {Num(s.wealthItems)}, buildings {Num(s.wealthBuildings)}, pawns {Num(s.wealthPawns)}).");
                if (s.colonyResources?.Count > 0)
                    sb.AppendLine("Stockpile: " + string.Join(", ", s.colonyResources.OrderByDescending(r => r.count).Take(10).Select(Counted)) + ".");
            }

            sb.AppendLine();
        }

        private static void AppendPawns(StringBuilder sb, List<IEffect> events, ColonySnapshot s)
        {
            List<PawnSnapshot> colonists = s.colonists ?? new List<PawnSnapshot>();
            if (colonists.Count == 0) return;

            sb.AppendLine($"Colonists ({colonists.Count}; valid targetPawn values): " +
                          string.Join(", ", colonists.Select(p => p.name)) + ".");

            sb.AppendLine("Best candidates for the consequences above:");
            foreach (PawnSnapshot pawn in PickCandidates(events, colonists))
                sb.AppendLine("  " + DescribePawn(pawn));
        }

        private static List<PawnSnapshot> PickCandidates(List<IEffect> events, List<PawnSnapshot> colonists)
        {
            var picked = new List<PawnSnapshot>();
            foreach (IEffect e in events.Where(e => e.Config.UsesTargetPawn))
            {
                Func<PawnSnapshot, float> score = _pawnScore.TryGetValue(e.Config.EffectName, out var fn) ? fn : Interest;

                foreach (PawnSnapshot pawn in colonists
                    .OrderByDescending(score).ThenBy(_ => _random.Next())   // random tie-break keeps prompts varied
                    .Take(DetailedPawnsPerEffect))
                {
                    if (!picked.Contains(pawn)) picked.Add(pawn);
                }
            }
            return picked.Take(MaxDetailedPawns).ToList();
        }

        // One line per pawn, only fields that carry information.
        private static string DescribePawn(PawnSnapshot p)
        {
            var parts = new List<string> { $"{p.name}, age {p.age}" };

            if (Has(p.current_action)) parts.Add("doing: " + p.current_action);
            if (p.queued_actions?.Count > 0) parts.Add("next: " + string.Join(" > ", p.queued_actions));
            if (Has(p.current_location)) parts.Add("in: " + p.current_location);
            if (p.is_drafted) parts.Add("drafted");
            if (Has(p.primary_weapon)) parts.Add("weapon: " + p.primary_weapon);

            PawnNeedsSnapshot n = p.needs;
            if (n != null)
            {
                parts.Add($"mood {Pct(n.mood)}, food {Pct(n.food_level)}, rest {Pct(n.rest_level)}, joy {Pct(n.recreation_level)}");
                if (Has(n.mental_state)) parts.Add("mental state: " + n.mental_state);
            }

            HealthSnapshot h = p.health;
            if (h != null)
            {
                parts.Add($"health {Pct(h.overall_health)}" + (h.is_downed ? " (downed)" : "") + (h.pain_level > 0.3f ? $", pain {Pct(h.pain_level)}" : ""));
                AddList(parts, "injuries", h.injuries);
                AddList(parts, "diseases", h.diseases);
                AddList(parts, "missing", h.missing_body_parts);
                AddList(parts, "implants", h.implants);
                AddList(parts, "chronic/scars", h.chronic_conditions);
            }

            if (p.traits?.Count > 0) parts.Add("traits: " + string.Join(", ", p.traits));
            if (p.skills?.Count > 0) parts.Add("skills: " + string.Join(", ", p.skills));

            string past = string.Join(" / ", new[] { p.childhood, p.adulthood }.Where(Has));
            if (past.Length > 0) parts.Add("past: " + past);

            PawnRelationsSnapshot r = p.relations;
            if (r != null)
            {
                if (r.lovers?.Count > 0) parts.Add("lovers: " + string.Join(", ", r.lovers));
                if (r.rivals?.Count > 0) parts.Add("rivals: " + string.Join(", ", r.rivals));
                if (Has(r.bonded_animal)) parts.Add("bonded animal: " + r.bonded_animal);
            }

            return string.Join("; ", parts);
        }

        private static void AppendTwist(StringBuilder sb)
        {
            double roll = _random.NextDouble();   // 60% chance of no twist
            if (roll < 0.20) sb.AppendLine("- Optional twist: find the irony. One clause only.");
            else if (roll < 0.35) sb.AppendLine("- Optional twist: one detail that doesn't add up. Don't explain it.");
            else if (roll < 0.40) sb.AppendLine("- Optional twist: one image that earns its beauty.");
        }

        // ----------------------------------------------------------------- scoring

        private static float Mood(PawnSnapshot p) => p.needs?.mood ?? 0.5f;

        private static float Interest(PawnSnapshot p)
        {
            float score = 0f;
            if (p.health?.is_downed == true) score += 10f;
            if (p.health?.diseases?.Count > 0) score += 5f;
            if (p.health?.missing_body_parts?.Count > 0) score += 4f;
            if (p.health?.injuries?.Count > 0) score += 3f;
            float mood = Mood(p);
            if (mood < 0.3f) score += 4f; else if (mood < 0.5f) score += 2f;
            return score;
        }

        // Skills are captured as "Shooting:12".
        private static float TopSkillLevel(PawnSnapshot p)
        {
            float best = 0f;
            if (p.skills == null) return best;
            foreach (string skill in p.skills)
            {
                int colon = skill.LastIndexOf(':');
                if (colon >= 0 &&
                    float.TryParse(skill.Substring(colon + 1), NumberStyles.Float, CultureInfo.InvariantCulture, out float level) &&
                    level > best)
                    best = level;
            }
            return best;
        }

        // ----------------------------------------------------------------- formatting helpers

        // The capturer writes "None"/"Unknown" instead of null, so treat those as empty.
        private static bool Has(string value) =>
            !string.IsNullOrWhiteSpace(value) &&
            !value.Equals("None", StringComparison.OrdinalIgnoreCase) &&
            !value.Equals("Unknown", StringComparison.OrdinalIgnoreCase);

        // Invariant formatting: P0 / N0 would use the player's culture ("45 %", nbsp separators).
        private static string Pct(float value) => Math.Round(value * 100f).ToString("0", CultureInfo.InvariantCulture) + "%";
        private static string Num(float value) => Math.Round(value).ToString("N0", CultureInfo.InvariantCulture);

        private static string Label(GameObjectMeta meta) =>
            !string.IsNullOrEmpty(meta?.Label) ? meta.Label : meta?.DefName ?? "unknown";

        private static string Counted(CountedMeta entry) => $"{entry.count}x {Label(entry.meta)}";

        private static void AddList(List<string> parts, string title, List<GameObjectMeta> items)
        {
            if (items?.Count > 0)
                parts.Add(title + ": " + string.Join(", ", items.Select(Label)));
        }
    }
}