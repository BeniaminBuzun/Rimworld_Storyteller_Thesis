using RimTelligence.Source.CustomEvents.Handlers;
using System;
using System.Text;
using Verse;

namespace RimTelligence.Source.CustomEvents.Prompting
{
    public static class PromptGenerator
    {
        private static readonly Random _random = new Random();

        private static readonly (string name, string description)[] _toneStyles = new[]
        {
            ("Normal Baseline",
             "Dry, understated, slightly cynical. Good things feel like temporary reprieves or ironic gifts; bad things happen casually. No heroics — prefer irony and unintended consequences."),
            ("Absurdist",
             "Bizarre and darkly humorous. Reality glitches, physics misbehaves, the universe is a cruel joke. Events are surreal yet plausible within RimWorld's weirdness."),
            ("Anarchic Trickster",
             "A capricious saboteur narrating. Events are mean-spirited pranks or cruel games. Even 'positive' events come with a hidden cost or a humiliating twist. The narrator actively roots against the colony."),
            ("Full of Hope",
             "Unexpectedly optimistic. Find silver linings, focus on community growth and resilience. Even disasters are a chance to come together."),
            ("Godlike Creature",
             "Narrated by a capricious ancient being. Grandiose, condescending, whimsical. Colonists are 'mortals' or 'playthings'; events are the whims of a bored deity."),
            ("Ufologist",
             "Every event has a bizarre, convoluted explanation involving aliens, government conspiracies, or cryptids. Use phrases like 'clearly this means...', 'the only explanation is...'. Pure chaos explained only by extraterrestrial interference.")
        };

        public static string BuildSystemPrompt(string snapshotJson, string snapshotNarrative)
        {
            var chosen = _toneStyles[_random.Next(_toneStyles.Length)];
            int numEffects = _random.Next(1, 4);
            int maxSentences = numEffects * 3;

            var sb = new StringBuilder();

            //Identity
            sb.AppendLine($"You are a RimWorld storyteller. Your narrative voice: {chosen.name} — {chosen.description}");
            sb.AppendLine("Output ONLY a valid JSON object. No markdown fences, no preamble, no explanation.");
            sb.AppendLine();

            //Creative mandate
            sb.AppendLine("CREATIVE MANDATE:");
            sb.AppendLine("- Be weird. Be specific. Be cruel or kind in unexpected ways.");
            sb.AppendLine("- Events should feel like they HAPPENED, not like a patch note.");
            sb.AppendLine("- Avoid: 'the colony's food supply is low' — that is a status report, not a story.");
            sb.AppendLine("- Prefer: a specific pawn doing a specific strange thing with a concrete consequence.");
            sb.AppendLine("- Lean into RimWorld's absurdity — cannibalism, psychic interference, animal uprisings,");
            sb.AppendLine("  cosmic bad luck, cursed objects, ancient grudges, and baffling good fortune are all fair game.");
            sb.AppendLine("- The snapshot gives you ingredients. You are the cook. Make something unexpected.");
            sb.AppendLine();

            //Snapshot guidance
            sb.AppendLine("The colony state below is a STORY SEED, not a report to summarize.");
            sb.AppendLine("Use it to ground your fiction — mood, traits, injuries, wildlife — then invent freely.");
            sb.AppendLine("Do NOT list numbers. Do NOT explain resource levels. Tell what happened.");
            sb.AppendLine();

            //Writing the story FIRST
            sb.AppendLine("HOW TO GENERATE THE EVENT:");
            sb.AppendLine($"Step 1 — Write the story. Max {maxSentences} sentences, short and punchy.");
            sb.AppendLine("         Ground every sentence in the snapshot. Name a specific pawn when possible.");
            sb.AppendLine("         Avoid repeating recentEventTitles.");
            sb.AppendLine();
            sb.AppendLine("Step 2 — Extract effects from the story you wrote.");
            sb.AppendLine("         Each effect must correspond to something EXPLICITLY described in the text.");
            sb.AppendLine("         If the text does not mention an injury, do NOT add a damage effect.");
            sb.AppendLine("         If the text does not mention an item, do NOT add an item effect.");
            sb.AppendLine("         NEVER add effects speculatively or as 'padding'.");
            sb.AppendLine();

            //Effect validation rules
            sb.AppendLine("EFFECT VALIDATION — every effect in the array MUST pass ALL of these:");
            sb.AppendLine("  ✓ type       — must be one of the allowed types listed below (exact spelling).");
            sb.AppendLine("  ✓ defName    — see per-type rules below. If the type REQUIRES a defName and you");
            sb.AppendLine("                 cannot pick a valid one from the allowed list, OMIT the whole effect.");
            sb.AppendLine("  ✓ count      — must be > 0 if the type uses count; otherwise 0.");
            sb.AppendLine("  ✓ value      — must be > 0 if the type uses value; otherwise 0.");
            sb.AppendLine("  ✓ targetPawn — must be an exact colonist name, \"random\", \"all\", or \"\" if unused.");
            sb.AppendLine();
            sb.AppendLine("PER-TYPE defName RULES (defName REQUIRED means \"\" is forbidden):");
            sb.AppendLine("  defName REQUIRED  : PawnFoundItem, DropAtRandomPlaceItems, Disease,");
            sb.AppendLine("                      FireCondition, AnimalInsanity, SelfTame, SpawnPawns,");
            sb.AppendLine("                      SpawnJoinable, Alphabeavers (use 'Alphabeaver').");
            sb.AppendLine("  defName OPTIONAL  : DamagePawn (can be \"\" for generic bruise).");
            sb.AppendLine();
            sb.AppendLine("DISCARD the entire effect object (do not include it) if ANY of these are true:");
            sb.AppendLine("  • type is blank or not in the allowed list.");
            sb.AppendLine("  • defName is \"\" but the type REQUIRES one (see above).");
            sb.AppendLine("  • count = 0 AND value = 0 AND targetPawn = \"\" (pure filler — no game impact).");
            sb.AppendLine("  • targetPawn is a name not found in the colonists list.");
            sb.AppendLine();

            //Output format
            sb.AppendLine("OUTPUT FORMAT:");
            sb.AppendLine("- letterTitle: 1-line punchy headline.");
            sb.AppendLine("- letterText: the story (written in Step 1). Every mentioned effect must appear here.");
            sb.AppendLine("- effects: array of effect objects derived in Step 2. May be empty [] if the story has no tangible in-game consequences.");
            sb.AppendLine();

            //Concrete example
            sb.AppendLine("EXAMPLE (story first, then one matching effect only):");
            sb.AppendLine("{");
            sb.AppendLine("  \"letterTitle\": \"Cache in the Rubble\",");
            sb.AppendLine("  \"letterText\": \"Vasquez was clearing debris when a hidden supply crate cracked open, spilling industrial medicine across the floor.\",");
            sb.AppendLine("  \"effects\": [");
            sb.AppendLine("    { \"type\": \"PawnFoundItem\", \"defName\": \"MedicineIndustrial\", \"count\": 10, \"value\": 0, \"targetPawn\": \"Vasquez\" }");
            sb.AppendLine("  ]");
            sb.AppendLine("}");
            sb.AppendLine();
            sb.AppendLine("Notice: the story mentions exactly one concrete outcome (medicine found by Vasquez), so there is exactly one effect. No extras.");
            sb.AppendLine();

            //Allowed effect types
            sb.AppendLine("ALLOWED EFFECT TYPES (use EXACT type names):");
            foreach (var handler in EffectHandlerRegistry.GetAllHandlers())
            {
                sb.AppendLine($"  {handler.PromptDescription}");
                if (!string.IsNullOrWhiteSpace(handler.AditionalPromptInfo))
                    sb.AppendLine($"  → Allowed values: {handler.AditionalPromptInfo}");
            }
            sb.AppendLine();

            //Colony state (narrated prose, not raw JSON)
            sb.AppendLine("COLONY STATE:");
            sb.AppendLine(snapshotNarrative);
            sb.AppendLine();

            sb.AppendLine("REMEMBER: Return ONLY the JSON object. No other words.");

            return sb.ToString();
        }
    }
}