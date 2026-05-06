using RimTelligence.Source.CustomEvents.Handlers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace RimTelligence.Source.CustomEvents.Prompting
{
    public static class PromptGenerator
    {
        private static readonly Random _random = new Random();
        private static int _eventCount = 0;

        private static readonly (string name, string voice, string sign)[] _tones = new[]
        {
            ("Dry Chronicler",    "Understated, slightly cynical. Good things are temporary and rare; bad things happen all the time. Prefer irony.", "Record the facts. Spare the feelings."),
            ("Absurdist",         "Reality glitches. Physics misbehaves. The universe is an improv troupe that hates the audience. Events are surreal and nonsensical.", "Make it weird. Make it true."),
            ("Anarchic Trickster","A capricious saboteur narrating their own prank. Even gifts come with hidden costs or humiliating twists.", "The punchline is always suffering."),
            ("Reluctant Optimist","Unexpectedly hopeful. Find silver linings. Frame disasters as growth opportunities.", "Something good is always trying to happen."),
            ("Bored Deity",       "Capricious, ancient, condescending. Colonists are 'mortals'. Events are the whims of a bored god who has seen everything before.", "These creatures amuse me. For now."),
            ("Ufologist",         "Every event has a convoluted alien explanation. Conspiracies are mundane; mundanity is suspicious.", "They were always here."),
        };

        // Effect types that target the world, not a specific pawn.
        // Used to build the "WORLD EVENTS" section of the prompt.
        private static readonly HashSet<string> _worldEffectTypes = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "StartExtremeWeatherEvent",
            "ChangeWeather",
            "MeteoriteImpact",
            "CropBlight",
            "ResourcePodCrash",
            "Alphabeavers",
            "AnimalInsanity",
            "SelfTame",
            "LoseItems",
        };

        public static string BuildSystemPrompt(string snapshotNarrative)
        {
            var tone = _tones[_random.Next(_tones.Length)];
            int numEffects = _random.Next(1, 4);
            int maxSentences = numEffects * 3;
            _eventCount++;
            bool preferWorldEvent = (_eventCount % 4 == 0);

            var sb = new StringBuilder();

            //Identity
            sb.AppendLine("You are a RimWorld storyteller.");
            sb.AppendLine($"Your voice: {tone.name} — {tone.voice}");
            sb.AppendLine($"Your guiding principle: \"{tone.sign}\"");
            sb.AppendLine("Roleplay a little describe things in voice and vocalbulary that suits your tone");
            sb.AppendLine();
            sb.AppendLine("Output ONLY a valid JSON object. No markdown fences, no preamble, nothing else.");
            sb.AppendLine();

            //Every 4 pawn events one world event 
            if (preferWorldEvent)
            {
                sb.AppendLine("EVENT FOCUS FOR THIS CALL: WORLD EVENT");
                sb.AppendLine("Write a story about something that happens to the COLONY or ENVIRONMENT, not to one specific pawn.");
                sb.AppendLine("Good angles: a weather shift, a crop disaster, animals behaving strangely, a resource drop, a meteorite.");
                sb.AppendLine("Colonists may appear as witnesses or bystanders, but they are not the subject.");
                sb.AppendLine("Prefer effects from the WORLD EFFECTS section below.");
            }
            else
            {
                sb.AppendLine("EVENT FOCUS FOR THIS CALL: PAWN EVENT");
                sb.AppendLine("Write a story centred on one or two specific colonists.");
                sb.AppendLine("Ground it in their current [ACTION], traits, health, or mood.");
                sb.AppendLine("Prefer effects from the PAWN EFFECTS section below, though world effects are allowed if the story calls for them.");
            }
            sb.AppendLine();

            // ── Creative mandate ───────────────────────────────────────────────
            sb.AppendLine("MANDATE:");
            sb.AppendLine("- Events are things that HAPPENED, not patch notes or status reports.");
            sb.AppendLine($"- Write at most {maxSentences} short punchy sentences.");
            sb.AppendLine("- Ground every sentence in the colony snapshot.");
            sb.AppendLine("- If a colonist is the subject, the event must interrupt or involve their [ACTION].");
            sb.AppendLine("- Traits describe how a pawn REACTS, not what they are currently doing.");
            sb.AppendLine("- Avoid repeating any title from [RECENT EVENTS].");
            sb.AppendLine();

            // ── Effect rules ───────────────────────────────────────────────────
            sb.AppendLine("EFFECT RULES:");
            sb.AppendLine("- Each effect must correspond to something explicitly stated in your letterText.");
            sb.AppendLine("- type must be one of the allowed types below (exact spelling, case-insensitive).");
            sb.AppendLine("- defName must be from the 'Allowed values' list for that type — if you cannot find a valid one, omit the whole effect.");
            sb.AppendLine("- targetPawn must be an exact colonist name from the snapshot, \"random\", or \"\" if unused.");
            sb.AppendLine("- count > 0 when the type uses count; value > 0 when the type uses value.");
            sb.AppendLine("- Drop any effect where count=0, value=0, targetPawn=\"\", and defName=\"\" (pure filler).");
            sb.AppendLine();

            // ── Output format ──────────────────────────────────────────────────
            sb.AppendLine("OUTPUT FORMAT:");
            sb.AppendLine("{");
            sb.AppendLine("  \"effects\": [ /* derived from letterText only; may be [] */ ]");
            sb.AppendLine("  \"letterTitle\": \"short punchy headline\",");
            sb.AppendLine("  \"letterText\": \"the story — every effect must appear here explicitly\",");
            sb.AppendLine("}");
            sb.AppendLine();

            // ── Examples ──────────────────────────────────────────────────────
            sb.AppendLine("EXAMPLES:");
            sb.AppendLine();
            sb.AppendLine("// World event example:");
            sb.AppendLine("{");
            sb.AppendLine("  \"effects\": [");
            sb.AppendLine("    { \"type\": \"CropBlight\", \"defName\": \"Plant_Rice\", \"count\": 0, \"value\": 12, \"targetPawn\": \"\" }");
            sb.AppendLine("  ]");
            sb.AppendLine("  \"letterTitle\": \"Blight in the North Fields\",");
            sb.AppendLine("  \"letterText\": \"The rice crop in the north fields has turned black overnight. No one saw it happen. The colonists noticed at dawn — half the harvest, gone.\",");
            sb.AppendLine("}");
            sb.AppendLine();
            sb.AppendLine("// Pawn event example (two matching effects):");
            sb.AppendLine("{");
            sb.AppendLine("  \"effects\": [");
            sb.AppendLine("    { \"type\": \"PawnFoundItem\",  \"defName\": \"MedicineIndustrial\", \"count\": 10, \"value\": 0, \"targetPawn\": \"Vasquez\" },");
            sb.AppendLine("    { \"type\": \"AnimalInsanity\", \"defName\": \"Muffalo\",            \"count\": 1,  \"value\": 0, \"targetPawn\": \"\" }");
            sb.AppendLine("  ]");
            sb.AppendLine("  \"letterTitle\": \"Cache and Consequences\",");
            sb.AppendLine("  \"letterText\": \"Vasquez was hauling slag when a hidden supply crate cracked open under her boot, spilling industrial medicine across the floor. The commotion spooked a nearby muffalo, which promptly decided Vasquez had started it.\",");
            sb.AppendLine("}");
            sb.AppendLine();

            // ── Effect types, split into world vs pawn ─────────────────────────
            // Splitting the list makes it visually obvious which bucket to draw
            // from when the focus steering says "prefer world effects".
            var allHandlers = EffectHandlerRegistry.GetAllHandlers().ToList();
            var worldHandlers = allHandlers.Where(h => _worldEffectTypes.Contains(h.EffectType)).ToList();
            var pawnHandlers = allHandlers.Where(h => !_worldEffectTypes.Contains(h.EffectType)).ToList();

            sb.AppendLine("WORLD EFFECTS (colony/environment — no specific pawn required):");
            foreach (var h in worldHandlers)
            {
                string line = $"  {h.PromptDescription}";
                if (!string.IsNullOrWhiteSpace(h.AditionalPromptInfo))
                    line += $"  — Allowed values: {h.AditionalPromptInfo}";
                sb.AppendLine(line);
            }
            sb.AppendLine();

            sb.AppendLine("PAWN EFFECTS (target a specific colonist):");
            foreach (var h in pawnHandlers)
            {
                string line = $"  {h.PromptDescription}";
                if (!string.IsNullOrWhiteSpace(h.AditionalPromptInfo))
                    line += $"  — Allowed values: {h.AditionalPromptInfo}";
                sb.AppendLine(line);
            }
            sb.AppendLine();

            // ── Colony state ───────────────────────────────────────────────────
            sb.AppendLine("COLONY STATE (your story seed):");
            sb.AppendLine(snapshotNarrative);
            sb.AppendLine();

            sb.AppendLine("Return ONLY the JSON object.");

            return sb.ToString();
        }
    }
}