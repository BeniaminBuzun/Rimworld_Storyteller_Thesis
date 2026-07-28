using RimTelligence.Source.CustomEvents.Handlers;
using RimTelligence.Source.DataSnapshots;
using RimTelligence.Source.Memory;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace RimTelligence.Source.CustomEvents.Prompting
{
    public static class ProperPromptGenerator
    {
        private static readonly Random _random = new Random();

        private static readonly (string name, string voice, string sign)[] _tones = new[]
        {
            ("Dry Chronicler",
             "Understated, slightly cynical. Prefer irony over drama. Facts first, feelings never.",
             "Record the facts. Spare the feelings."),

            ("Absurdist",
             "Reality misbehaves. Cause and effect are suggestions. Use one image that makes no logical sense but feels completely true.",
             "Make it weird. Make it true."),

            ("Anarchic Trickster",
             "Narrating your own prank. Gifts have hidden costs. Good news arrives in bad packaging. Use second person occasionally.",
             "The punchline is always suffering."),

            ("Reluctant Optimist",
             "You want to be hopeful. You're trying very hard. The disaster is reframed as opportunity but you don't quite believe it yourself.",
             "Something good is always trying to happen."),

            ("Bored Deity",
             "Ancient, condescending, mildly entertained. Call colonists 'mortals'. Use passive constructions as if events simply occur.",
             "These creatures amuse me. For now."),

            ("Ufologist",
             "Everything connects to something larger. Mundane events have sinister origins. One unexplained technical detail per event.",
             "They were always here."),

            ("Field Medic",
             "Clipped, clinical, matter-of-fact. You've seen worse. Emotions are diagnoses.",
             "Triage everything. Mourn nothing."),

            ("Tired Poet",
             "Lyrical but exhausted. Beauty in collapse. Short sentences. One image that lingers.",
             "Even ruin has a shape."),
        };

        public static string BuildSystemPrompt(
            string fullSnapshotNarrative,
            List<SelectedEventIntent> chosenEvents,
            ColonySnapshot snapshot = null,
            RagQueryResult ragContext = null)
        {
            var tone = _tones[_random.Next(_tones.Length)];

            // 1 effect = 2 sentences, 2 effects = 3 sentences, 3+ = 4 sentences. Hard ceiling.
            int maxSentences = Math.Max(chosenEvents.Count * 2, 4);

            var sb = new StringBuilder();

            //
            //
            //
            //
            //  Identity 
            sb.AppendLine("You are a RimWorld storyteller narrator. You are terse and distinctive.");
            sb.AppendLine($"Voice: {tone.name} — {tone.voice}");
            sb.AppendLine($"Principle: \"{tone.sign}\"");
            sb.AppendLine("Every word must sound like YOU. Not a patch note. Not a status report.");
            sb.AppendLine("Be creative within those constraints. Make the story sing. Make it feel like a moment from a play or a novel, not a game log.");
            sb.AppendLine();
            sb.AppendLine("Output ONLY a valid JSON object. No markdown fences, no preamble.");
            sb.AppendLine();

            //  Cue 
            sb.AppendLine("DIRECTOR'S CUE — these events happened:");
            foreach (var evt in chosenEvents)
                sb.AppendLine($"  - {evt.type} -");
            sb.AppendLine();

            //  Writing rules 
            sb.AppendLine("WRITING RULES:");
            sb.AppendLine($"- The letter must tell one tiny story, not a list of facts.");
            sb.AppendLine($"- Every sentence must follow from the one before it. Cause and effect, or a single unfolding moment.");
            sb.AppendLine($"- If multiple events happened, show how one led to the other. Never just 'X happened. Y happened. Z happened.'");
            sb.AppendLine($"- letterText is {maxSentences} sentences. Exactly. Not one more.");
            sb.AppendLine("- letterTitle: 2-4 words, no punctuation.");
            sb.AppendLine();

            //  Colonist rules — this is the critical section 
            sb.AppendLine("COLONIST RULES (strictly enforced):");
            sb.AppendLine("- Each effect names a targetPawn or is a world event.");
            sb.AppendLine("- targetPawn effect: ONLY that pawn appears. Zero other colonists. Not by name, not by implication.");
            sb.AppendLine("- If the targetPawn's current [ACTION] is incompatible with the event (e.g. sleeping and finding an item), TWIST their [ACTION] so it sounds plausible.");
            sb.AppendLine("- A colonist may only appear if they CAUSED, DISCOVERED, or were INTERRUPTED BY the event.");
            sb.AppendLine("- Background presence, from uninvolved colonists are FORBIDDEN.");
            sb.AppendLine("- The rat, the sleeping pawn, the nearby colonist — if they didn't touch the event, they don't exist.");
            sb.AppendLine();

            //  Atmosphere 
            AppendStoryContextPrompts(sb, snapshot);

            //  Effect rules 
            sb.AppendLine("EFFECT RULES:");
            sb.AppendLine("- Every effect from the cue must appear in the effects array.");
            sb.AppendLine("- Every effect gets one clause woven into letterText. One clause. Not a sentence.");
            sb.AppendLine("- defName must come strictly from Allowed Values below.");
            sb.AppendLine("- value: LOW/MEDIUM/HIGH when the type uses value. count > 0 when the type uses count.");
            sb.AppendLine();

            //  Output format 
            sb.AppendLine("OUTPUT FORMAT:");
            sb.AppendLine("{");
            sb.AppendLine("  \"letterTitle\": \"2-4 word title\",");
            sb.AppendLine("  \"letterText\": \"narrative here\",");
            sb.AppendLine("  \"effects\": [");
            sb.AppendLine("    {");
            sb.AppendLine("      \"type\": \"ExactTypeFromCue\",");
            sb.AppendLine("      \"targetPawn\": \"PawnName or \\\"\\\"\",");
            sb.AppendLine("      \"defName\": \"ChosenDefName\",");
            sb.AppendLine("      \"count\": 0,");
            sb.AppendLine("      \"value\": \"NONE\"");
            sb.AppendLine("    }");
            sb.AppendLine("  ]");
            sb.AppendLine("}");
            sb.AppendLine();

            //  Allowed values 
            sb.AppendLine("ALLOWED VALUES:");
            var allHandlers = EffectHandlerRegistry.GetAllHandlers();
            foreach (var chosen in chosenEvents)
            {
                var handler = allHandlers.FirstOrDefault(h =>
                    string.Equals(h.EffectName, chosen.type, StringComparison.OrdinalIgnoreCase));
                if (handler == null) continue;

                sb.AppendLine($"- {handler.EffectName}: {handler.PromptStructure}");
                if (!string.IsNullOrWhiteSpace(handler.AditionalPromptStructureInfo))
                    sb.AppendLine($"  Allowed: {handler.AditionalPromptStructureInfo}");
            }
            sb.AppendLine();

            //  Colony state 
            sb.AppendLine("COLONY STATE:");
            sb.AppendLine(fullSnapshotNarrative);
            sb.AppendLine();

            AppendRagContext(sb, ragContext);

            sb.AppendLine("Return ONLY the JSON object.");
            return sb.ToString();
        }

        private static void AppendRagContext(StringBuilder sb, RagQueryResult ragContext)
        {
            if (ragContext == null || !ragContext.HasResults)
                return;

            sb.AppendLine("RELEVANT CONTEXT (past events / lore relevant to this moment):");
            foreach (var r in ragContext.StaticResults)
                sb.AppendLine($"- [Static] {r.Source} (Score: {r.Score:F2}): {r.Text}");
            foreach (var r in ragContext.DynamicResults)
                sb.AppendLine($"- [Dynamic] {r.Source} (Score: {r.Score:F2}): {r.Text}");
            sb.AppendLine();
        }

        private static void AppendStoryContextPrompts(StringBuilder sb, ColonySnapshot s)
        {
            if (s == null || s.colonists == null || s.colonists.Count == 0)
                return;

            float avgMood = s.colonists.Average(p => p.needs?.mood ?? 0.5f);
            bool isWinter = s.season?.Contains("Winter") ?? false;
            bool isNight = TimeSpan.TryParse(s.time, out TimeSpan t)
                            && (t.Hours >= 22 || t.Hours < 6);

            // One atmosphere note, written as creative direction not a rule
            string note = null;
            if (avgMood < 0.25f) note = "These people are close to breaking. Let that bleed through without stating it.";
            else if (avgMood > 0.75f) note = "They're almost happy. Make that feel precarious.";
            else if (s.daysSinceLastRaid <= 2) note = "The last raid isn't over in their heads. One detail of that rawness.";
            else if (isWinter) note = "The cold is a character. One physical sensation only.";
            else if (isNight) note = "Dark outside. One sound or shadow. Nothing more.";
            else if (s.colonistCount <= 3) note = "Few people. Every event lands harder. One internal beat.";

            if (note != null)
            {
                sb.AppendLine($"ATMOSPHERE: {note}");
                sb.AppendLine();
            }

            // At most one optional twist — 60% chance of nothing
            double roll = _random.NextDouble();
            if (roll < 0.20) sb.AppendLine("OPTIONAL TWIST: Find the irony. One clause only.");
            else if (roll < 0.35) sb.AppendLine("OPTIONAL TWIST: One detail that doesn't add up. Don't explain it.");
            else if (roll < 0.40) sb.AppendLine("OPTIONAL TWIST: One image that earns its beauty.");

            sb.AppendLine();
        }
    }
}