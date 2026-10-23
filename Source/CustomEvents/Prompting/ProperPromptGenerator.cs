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

        private static void AppendTwist(StringBuilder sb)
        {
            double roll = _random.NextDouble();   // 60% chance of no twist
            if (roll < 0.20) sb.AppendLine("- Optional twist: find the irony. One clause only.");
            else if (roll < 0.35) sb.AppendLine("- Optional twist: one detail that doesn't add up. Don't explain it.");
            else if (roll < 0.40) sb.AppendLine("- Optional twist: one image that earns its beauty.");
        }
    }
}