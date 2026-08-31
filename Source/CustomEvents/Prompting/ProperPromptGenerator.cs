using RimTelligence.CustomEvents.Handlers;
using RimTelligence.DataSnapshots;
using RimTelligence.Memory;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace RimTelligence.CustomEvents.Prompting
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
            List<IEffect> chosenEvents,
            ColonySnapshot snapshot = null)
        {
            var tone = _tones[_random.Next(_tones.Length)];

            // 1 effect = 2 sentences, 2 effects = 3 sentences, 3+ = 4 sentences. Hard ceiling.
            int maxSentences = Math.Max(chosenEvents.Count * 2, 4);

            var sb = new StringBuilder();

            //  Identity 
            sb.AppendLine("You are a RimWorld storyteller narrator. You are terse and distinctive.");
            sb.AppendLine($"Voice: {tone.name} — {tone.voice}");
            sb.AppendLine($"Principle: \"{tone.sign}\"");
            sb.AppendLine("Every word must sound like YOU. Not a patch note. Not a status report.");
            sb.AppendLine();
            sb.AppendLine("Output ONLY a valid JSON object. No markdown fences, no preamble.");
            sb.AppendLine();

            //  Cue 
            sb.AppendLine("DIRECTOR'S CUE — these events happened (use exactly these effectName values):");
            foreach (var evt in chosenEvents)
                sb.AppendLine($"  - {evt.Config.EffectName} -");
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
            sb.AppendLine("- The 'effects' array MUST contain exactly the effects from the cue, and MUST NOT be empty.");
            sb.AppendLine("- Every effect MUST set 'effectName' to the EXACT name given in the cue / Allowed Values below.");
            sb.AppendLine("- 'selectedDefName' must come strictly from that effect's Allowed Values below.");
            sb.AppendLine("- Include 'severity' only for effects that show 'severity:' in Allowed Values. Use LOW/MEDIUM/HIGH/EXTREME.");
            sb.AppendLine("- Include 'targetPawn' only for effects that show 'targetPawn:' in Allowed Values.");
            sb.AppendLine();
            sb.AppendLine("EFFECT JSON SCHEMA (the ONLY allowed keys):");
            sb.AppendLine("{\n  \"effectName\": \"<exact effect name from cue>\",\n  \"selectedDefName\": [\"<defName>\"] | [\"<defName>\", \"<second defName>\"],\n  \"severity\": \"LOW|MEDIUM|HIGH|EXTREME\",\n  \"targetPawn\": \"<colonist name>\"\n}");
            sb.AppendLine("- 'severity' and 'targetPawn' are optional; include them only when that effect's Allowed Values say so.");
            sb.AppendLine();

            //  Allowed values 
            sb.AppendLine("ALLOWED VALUES:");
            foreach (var chosen in chosenEvents)
            {
                EffectConfig contract = EffectContractProvider.GetContract(chosen, snapshot);

                sb.AppendLine($"- {contract.EffectName}: {contract.NarrativeDescription}");

                if (contract.UsesSeverity)
                    sb.AppendLine("    severity: LOW/MEDIUM/HIGH/EXTREME");
                if (contract.UsesTargetPawn)
                    sb.AppendLine("    targetPawn: one of the colonists available in colony state");

                if (contract.IsDefRequired && contract.AllowedDefs.Count > 0)
                    sb.AppendLine($"    allowed defName: {FormatDefList(contract.AllowedDefs)}");
                else if (contract.IsDefRequired)
                    sb.AppendLine("    allowed defName: any defName chosen by you (free text)");

                if (contract.SecondaryAllowedDefs.Count > 0)
                    sb.AppendLine($"    second defName (body part): {FormatDefList(contract.SecondaryAllowedDefs)}");

                sb.AppendLine();
            }
            sb.AppendLine();
            //  Colony state 
            sb.AppendLine("COLONY STATE:");
            sb.AppendLine(fullSnapshotNarrative);
            sb.AppendLine();

            AppendStoryContextPrompts(sb, snapshot);

            sb.AppendLine("Return ONLY the JSON object.");
            return sb.ToString();
        }

        private static void AppendStoryContextPrompts(StringBuilder sb, ColonySnapshot snapshot)
        {
            if (snapshot == null || snapshot.colonists == null || snapshot.colonists.Count == 0)
                return;

            float avgMood = snapshot.colonists.Average(pawn => pawn.needs?.mood ?? 0.5f);
            bool isWinter = snapshot.season?.Contains("Winter") ?? false;
            bool isNight = TimeSpan.TryParse(snapshot.time, out TimeSpan time)
                            && (time.Hours >= 22 || time.Hours < 6);

            // At most one optional twist — 60% chance of nothing
            double roll = _random.NextDouble();
            if (roll < 0.20) sb.AppendLine("OPTIONAL TWIST: Find the irony. One clause only.");
            else if (roll < 0.35) sb.AppendLine("OPTIONAL TWIST: One detail that doesn't add up. Don't explain it.");
            else if (roll < 0.40) sb.AppendLine("OPTIONAL TWIST: One image that earns its beauty.");

            sb.AppendLine();
        }

        private static string FormatDefList(List<GameObjectMeta> defs)
        {
            const int MaxDefs = 5;

            List<GameObjectMeta> sample = defs.Count > MaxDefs
                ? defs.OrderBy(_ => _random.Next()).Take(MaxDefs).ToList()
                : defs;

            var sb = new StringBuilder();
            for (int index = 0; index < sample.Count; index++)
            {
                if (index > 0)
                    sb.Append(", ");
                GameObjectMeta def = sample[index];
                sb.Append(def.DefName);
                if (!string.IsNullOrEmpty(def.Label) && def.Label != def.DefName)
                    sb.Append(" (").Append(def.Label).Append(")");
            }
            return sb.ToString();
        }
    }
}