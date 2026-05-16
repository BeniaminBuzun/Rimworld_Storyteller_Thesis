using RimTelligence.Source.CustomEvents.Handlers;
using RimTelligence.Source.DataSnapshots;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace RimTelligence.Source.CustomEvents
{
    public class SelectedEventIntent
    {
        public string type { get; set; }
    }

    public static class InitialPromptGenerator
    {
        private static readonly Random _random = new Random();
        private static int _eventCount = 0;

        private static readonly HashSet<string> _worldEffectFamilies = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "ResourcePodCrash", "LoseItems", "StartExtremeWeatherEvent",
            "ChangeWeather", "MeteoriteImpact", "CropBlight",
            "AnimalInsanity", "SelfTame", "SpawnAnimals"
        };

        public static string BuildSystemPrompt(string briefSnapshotNarrative, ColonySnapshot snapshot = null)
        {
            int numEffects = _random.Next(1, 5);
            _eventCount++;
            bool preferWorldEvent = (_eventCount % 4 == 0);

            var sb = new StringBuilder();

            sb.AppendLine("You are an AI RimWorld Storyteller (Event Director).");
            sb.AppendLine("Your job is to look at the high-level state of the colony and decide WHAT should happen next.");
            sb.AppendLine("Output ONLY a valid JSON object. No markdown fences, no preamble.");
            sb.AppendLine();

            if (preferWorldEvent)
            {
                sb.AppendLine("EVENT FOCUS FOR THIS CALL: WORLD / COLONY EVENT");
                sb.AppendLine("Prefer selecting events that affect the environment, resources, or the colony as a whole.");
            }
            else
            {
                sb.AppendLine("EVENT FOCUS FOR THIS CALL: PAWN EVENT");
                sb.AppendLine("Prefer selecting events that target specific colonists based on their current needs or health.");
            }
            sb.AppendLine();

            //  CONTEXTUAL PROMPTS (chance & logic) 
            AppendContextualPrompts(sb, snapshot);

            //  Tone nudges for event count 
            if (_eventCount % 10 == 0)
            {
                sb.AppendLine("NARRATIVE NUDGE: The colony could use a moment of relief. Lean toward a GOOD event this time.");
                sb.AppendLine("You may select a Good event even if the colony state is stable.");
                sb.AppendLine();
            }
            if (_eventCount == 7)
            {
                sb.AppendLine("NARRATIVE NUDGE: A challenge is overdue. Lean toward a BAD event this time.");
                sb.AppendLine("You may select a Bad event even if the colony state is safe.");
                sb.AppendLine();
            }

            //  Instructions 
            sb.AppendLine("INSTRUCTIONS:");
            sb.AppendLine($"- Select between 1 and {numEffects} event types from the ALLOWED EVENT TYPES list below.");
            sb.AppendLine("- Provide a brief \"reason\" for why this event fits the current colony state.");
            sb.AppendLine("- DO NOT select target pawns. You are only setting the high-level plot.");
            sb.AppendLine();

            //  Output format 
            sb.AppendLine("OUTPUT FORMAT:");
            sb.AppendLine("{");
            sb.AppendLine("  \"selected_events\": [");
            sb.AppendLine("    {");
            sb.AppendLine("      \"type\": \"ExactEventTypeName\"");
            sb.AppendLine("    }");
            sb.AppendLine("  ]");
            sb.AppendLine("}");
            sb.AppendLine();

            var allHandlers = EffectHandlerRegistry.GetAllHandlers().ToList();

            var worldHandlers = allHandlers.Where(h =>
                _worldEffectFamilies.Contains(h.EffectName.Split('_')[0])).ToList();

            var pawnHandlers = allHandlers.Where(h =>
                !_worldEffectFamilies.Contains(h.EffectName.Split('_')[0])).ToList();

            sb.AppendLine("ALLOWED WORLD EVENTS (No specific pawn required):");
            foreach (var h in worldHandlers)
                sb.AppendLine($"  - {h.EffectName}: {h.NarrativeDescription}");
            sb.AppendLine();

            sb.AppendLine("ALLOWED PAWN EVENTS (Requires a specific pawn, which will be chosen later):");
            foreach (var h in pawnHandlers)
                sb.AppendLine($"  - {h.EffectName}: {h.NarrativeDescription}");
            sb.AppendLine();

            //  Brief Colony State 
            sb.AppendLine("COLONY STATE (Brief Overview):");
            sb.AppendLine(briefSnapshotNarrative);
            sb.AppendLine();

            sb.AppendLine("Return ONLY the JSON object.");

            return sb.ToString();
        }

        /// <summary>
        /// Injects conditional prompts: logic‑based (colony state) and chance‑based (storyteller whims).
        /// The "no loot events" directive now appears only with a 60% random chance.
        /// </summary>
        private static void AppendContextualPrompts(StringBuilder sb, ColonySnapshot s)
        {
            // ---- 60% chance: include the anti‑loot directive -------------------------
            if (_random.NextDouble() < 0.6)
            {
                sb.AppendLine("EVENT VARIETY & REALISM:");
                sb.AppendLine("- Do NOT choose multiple events of the same kind (e.g., several 'find item' events in one call).");
                sb.AppendLine("- Resource‑spawning events (like ResourcePodCrash, finding meals or medicine) are EXCEPTIONALLY RARE.");
                sb.AppendLine("  Use them ONLY when the colony is critically low on that resource AND no similar spawn has happened recently.");
                sb.AppendLine("- The story should NOT feel like a loot generator. Prefer events that create tension, social drama,");
                sb.AppendLine("  health crises, mental breaks, weather shifts, or mysterious phenomena.");
                sb.AppendLine("- If the snapshot mentions recent items being found, break the pattern with something completely different.");
                sb.AppendLine();
            }

            // ---- Data‑driven prompts (only if snapshot is available) -----------------
            if (s == null || s.colonists == null || s.colonists.Count == 0)
                return;

            // Mood
            float avgMood = s.colonists.Average(p => p.needs?.mood ?? 0.5f);
            if (avgMood < 0.25f)
            {
                sb.AppendLine("CRITICAL MOOD: The colonists are on edge. Events that trigger social fights, mental breaks, or desperate actions are highly fitting.");
                sb.AppendLine();
            }

            // Active mental breaks
            bool anyMentalBreak = s.colonists.Any(p => !string.IsNullOrEmpty(p.needs?.mental_state) && p.needs.mental_state != "Normal");
            if (anyMentalBreak)
            {
                sb.AppendLine("MENTAL INSTABILITY: At least one colonist is already in a mental break. The situation could spiral — consider events that deepen the chaos.");
                sb.AppendLine();
            }

            // Food
            if (s.totalNutrition < 10f)
            {
                sb.AppendLine("STARVATION THREAT: Food supplies are dangerously low. Events around food — desperate hunting, strange edible finds, or conflicts over meals — feel appropriate.");
                sb.AppendLine();
            }

            // Downed colonist
            bool hasDowned = s.colonists.Any(p => p.health?.is_downed == true);
            if (hasDowned)
            {
                sb.AppendLine("MEDICAL CRISIS: A colonist is downed. Medical emergencies, infections, or even miraculous recoveries can take centre stage.");
                sb.AppendLine();
            }

            // Raid recency
            if (s.daysSinceLastRaid >= 15)
            {
                sb.AppendLine("UNEASY PEACE: It's been too long without a major threat. A Bad event — maybe a raid, a manhunting pack, or a saboteur — feels due.");
                sb.AppendLine();
            }
            else if (s.daysSinceLastRaid <= 2)
            {
                sb.AppendLine("RAW WOUNDS: A recent raid just happened. Avoid another immediate assault; focus on aftermath: injuries, mental strain, rebuilding, or internal scapegoating.");
                sb.AppendLine();
            }

            // Wealth
            if (s.wealthTotal > 100000)
            {
                sb.AppendLine("PROSPERITY ATTRACTS DANGER: The colony's wealth makes it a tempting target. Raids, thieves, or jealous factions are on the horizon.");
                sb.AppendLine();
            }

            // Night
            if (!string.IsNullOrEmpty(s.time) && TimeSpan.TryParse(s.time, out TimeSpan t))
            {
                if (t.Hours >= 22 || t.Hours < 6)
                {
                    sb.AppendLine("UNDER COVER OF DARKNESS: Night events should feel spookier — predators, strange signals, sleepwalking, or shadowy visitors.");
                    sb.AppendLine();
                }
            }

            // Winter
            if (!string.IsNullOrEmpty(s.season) && s.season.Contains("Winter"))
            {
                sb.AppendLine("WINTER'S GRIP: The cold changes everything. Events that involve freezing, hypothermia, frozen crops, or desperate survival choices fit well.");
                sb.AppendLine();
            }

            // Active conditions
            if (s.activeConditions != null && s.activeConditions.Any())
            {
                string cond = string.Join(", ", s.activeConditions);
                sb.AppendLine($"ENVIRONMENTAL PRESSURE: Active conditions ({cond}) are already straining the colony. Events should interact with this reality — no sunshine picnics during toxic fallout.");
                sb.AppendLine();
            }

            // Small colony
            if (s.colonistCount <= 2)
            {
                sb.AppendLine("LONELY OUTPOST: Only a handful of colonists. Every event feels more personal, more intense. No indiscriminate mass disasters — pick something intimate.");
                sb.AppendLine();
            }

            // Many animals
            if (s.colonyAnimalCount >= 10)
            {
                sb.AppendLine("MENAGERIE: A large animal population can invite diseases, predator attacks, or stampedes.");
                sb.AppendLine();
            }

            // ---- Chance‑based illusory prompts (independent of snapshot) ----
            if (_random.NextDouble() < 0.3)
            {
                sb.AppendLine("STORYTELLER'S WHIM: Today, the universe feels capricious. Events may carry an ironic or absurd twist.");
                sb.AppendLine();
            }

            if (_random.NextDouble() < 0.15)
            {
                sb.AppendLine("SILVER LINING: Fate offers a brief, unearned kindness. Consider picking a Good event even if nothing is desperate.");
                sb.AppendLine();
            }

            if (_random.NextDouble() < 0.2)
            {
                sb.AppendLine("OMINOUS SIGNS: The wind carries a chill. Today, even mundane events should feel foreboding.");
                sb.AppendLine();
            }

            if (_random.NextDouble() < 0.1)
            {
                sb.AppendLine("BORED DEITY: The storyteller yawns. Pick an event you haven't used in a while. Surprise them.");
                sb.AppendLine();
            }

            if (_random.NextDouble() < 0.05)
            {
                sb.AppendLine("MYSTERY BOX: Something utterly unexplainable happens. An event that raises questions, not answers.");
                sb.AppendLine();
            }
        }
    }
}