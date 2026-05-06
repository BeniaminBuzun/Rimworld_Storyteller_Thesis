using RimTelligence.Source.DataSnapshots;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace RimTelligence.Source.CustomEvents.Prompting
{
   public static class SnapshotNarrator
    {
        private static readonly Random _rng = new Random();

        private const int MaxResourceEntries = 10;
        private const int SpotlightCount = 2;   
        private const int BriefCount = 999;

        public static string Narrate(ColonySnapshot s)
        {
            var sb = new StringBuilder();

            sb.Append($"Colony \"{s.colonyName}\", day {s.day}");
            sb.Append($" — {s.season?.ToLower() ?? "unknown season"}, {s.biome?.ToLower() ?? "hostile biome"}.");
            sb.Append($" Outside: {DescribeTemperature(s.temperature)}, {s.currentWeather?.ToLower() ?? "unclear weather"}.");
            sb.AppendLine();


            if (s.colonists != null && s.colonists.Count > 0)
            {
                var names = s.colonists.Select(p => p.name).Where(n => !string.IsNullOrWhiteSpace(n)).ToList();
                sb.AppendLine($"Colonists ({s.colonistCount} total, exact names for targetPawn): {string.Join(", ", names)}.");
                sb.AppendLine();

                var ordered = s.colonists.OrderByDescending(InterestScore).ToList();
                var spotlight = ordered.Take(SpotlightCount).ToList();
                var brief = ordered.Skip(SpotlightCount).ToList();

                sb.AppendLine("Key colonists (full detail):");
                foreach (var p in spotlight)
                    sb.AppendLine("  " + DescribePawnFull(p));

                if (brief.Count > 0)
                {
                    sb.AppendLine("Other colonists (brief):");
                    foreach (var p in brief)
                        sb.AppendLine("  " + DescribePawnBrief(p));
                }
                sb.AppendLine();
            }

            sb.AppendLine(DescribeFood(s.totalNutrition, s.colonistCount));
            sb.AppendLine(DescribeWealth(s.wealthTotal));
            sb.AppendLine(DescribeRaidPressure(s.daysSinceLastRaid));

            if (s.activeConditions != null && s.activeConditions.Count > 0)
                sb.AppendLine($"Active conditions: {string.Join(", ", s.activeConditions)}.");

            if (s.wildAnimals != null && s.wildAnimals.Count > 0)
            {
                var sample = s.wildAnimals
                    .OrderByDescending(kv => kv.Value)
                    .Take(6)
                    .Select(kv => $"{kv.Value}x {kv.Key}")
                    .ToList();
                sb.AppendLine($"Wildlife nearby: {string.Join(", ", sample)}.");
            }

            if (s.cultivatedPlants != null && s.cultivatedPlants.Count > 0)
            {
                var crops = s.cultivatedPlants
                    .Select(kv => $"{kv.Value}x {kv.Key}")
                    .ToList();
                sb.AppendLine($"Crops: {string.Join(", ", crops)}.");
            }

            if (s.resources?.allResources != null && s.resources.allResources.Count > 0)
            {
                var topResources = s.resources.allResources
                    .OrderByDescending(kv => kv.Value)
                    .Take(MaxResourceEntries)
                    .Select(kv => $"{kv.Key}: {kv.Value}");
                string suffix = s.resources.allResources.Count > MaxResourceEntries
                    ? $" (+ {s.resources.allResources.Count - MaxResourceEntries} more)"
                    : "";
                sb.AppendLine($"Resources: {string.Join(", ", topResources)}{suffix}.");
            }

            if (s.recentEventTitles != null && s.recentEventTitles.Count > 0)
                sb.AppendLine($"[RECENT EVENTS — do not repeat]: {string.Join("; ", s.recentEventTitles)}.");

            return sb.ToString().Trim();
        }

        private static float InterestScore(PawnSnapshot p)
        {
            float score = 0f;
            if (p.health?.is_downed == true) score += 10f;
            if (p.health?.diseases?.Count > 0) score += 5f;
            if (p.health?.injuries?.Count > 0) score += 3f;
            if (p.health?.missing_body_parts?.Count > 0) score += 4f;
            if (p.mood < 0.3f) score += 4f;
            else if (p.mood < 0.5f) score += 2f;
            if (p.traits?.Count > 0) score += 1f;
            return score;
        }

        private static string DescribePawnFull(PawnSnapshot p)
        {
            var sb = new StringBuilder();
            sb.Append($"{p.name} (age {p.age})");

            if (!string.IsNullOrWhiteSpace(p.current_action))
                sb.Append($" [ACTION: {p.current_action.ToLower()}]");

            if (p.queued_actions?.Count > 0)
                sb.Append($" [QUEUE: {string.Join(" → ", p.queued_actions).ToLower()}]");

            sb.Append($" [MOOD: {DescribeMood(p.mood)}]");

            if (p.health != null)
            {
                sb.Append($" [HEALTH: {p.health.overall_health:P0}");
                if (p.health.is_downed) sb.Append(", DOWNED");
                if (p.health.pain_level > 0.3f) sb.Append($", pain {p.health.pain_level:P0}");
                if (p.health.injuries?.Count > 0) sb.Append($", injuries: {string.Join(", ", p.health.injuries)}");
                if (p.health.diseases?.Count > 0) sb.Append($", diseases: {string.Join(", ", p.health.diseases)}");
                if (p.health.missing_body_parts?.Count > 0) sb.Append($", missing: {string.Join(", ", p.health.missing_body_parts)}");
                if (p.health.implants?.Count > 0) sb.Append($", implants: {string.Join(", ", p.health.implants)}");
                if (p.health.chronic_conditions?.Count > 0) sb.Append($", chronic: {string.Join(", ", p.health.chronic_conditions)}");
                sb.Append("]");
            }

            if (p.traits?.Count > 0)
                sb.Append($" [TRAITS: {string.Join(", ", p.traits)}]");

            // Background only in full detail
            if (!string.IsNullOrWhiteSpace(p.childhood) || !string.IsNullOrWhiteSpace(p.adulthood))
                sb.Append($" [PAST: {p.childhood}/{p.adulthood}]");

            if (p.skills?.Count > 0)
                sb.Append($" [SKILLS: {string.Join(", ", p.skills)}]");

            return sb.ToString();
        }

        private static string DescribePawnBrief(PawnSnapshot p)
        {
            var sb = new StringBuilder();
            sb.Append($"{p.name}");

            if (!string.IsNullOrWhiteSpace(p.current_action))
                sb.Append($" [ACTION: {p.current_action.ToLower()}]");

            sb.Append($" [MOOD: {DescribeMood(p.mood)}]");

            if (p.health?.is_downed == true) sb.Append(" [DOWNED]");
            if (p.health?.diseases?.Count > 0) sb.Append($" [SICK: {string.Join(", ", p.health.diseases)}]");
            if (p.traits?.Count > 0) sb.Append($" [TRAITS: {string.Join(", ", p.traits)}]");

            return sb.ToString();
        }


        private static string DescribeMood(float mood)
        {
            if (mood < 0.25f) return $"{mood:P0} (breaking)";
            if (mood < 0.45f) return $"{mood:P0} (miserable)";
            if (mood < 0.65f) return $"{mood:P0} (stable)";
            if (mood < 0.80f) return $"{mood:P0} (good)";
            return $"{mood:P0} (euphoric)";
        }

        private static string DescribeFood(float nutrition, int colonistCount)
        {
            if (colonistCount <= 0) return "Food situation unknown.";
            float daysLeft = nutrition / Math.Max(colonistCount * 1.6f, 1f);
            if (daysLeft < 2) return "The colony is starving. Meals are rationed to crumbs.";
            if (daysLeft < 5) return "Food is dangerously low — hunger is a constant companion.";
            if (daysLeft < 12) return "Provisions are modest; nobody starving, nobody comfortable.";
            if (daysLeft < 25) return "Food stocks are adequate.";
            return "The pantry is embarrassingly full. Abundance breeds complacency.";
        }

        private static string DescribeWealth(float wealth)
        {
            if (wealth < 15000) return "The colony is dirt poor — scavenged furniture and desperate hope.";
            if (wealth < 40000) return "Scraping by: enough to survive, little to spare.";
            if (wealth < 100000) return "A modest, functional colony — not impressive, but standing.";
            if (wealth < 250000) return "The colony has accumulated real wealth. Comfortable, almost.";
            return "Grotesquely wealthy by rimworld standards. A tempting target.";
        }

        private static string DescribeRaidPressure(int daysSince)
        {
            if (daysSince < 0) return "Raiders have not come yet — but they will.";
            if (daysSince == 0) return "Raiders struck today. The blood is still fresh.";
            if (daysSince < 4) return "The last raid was only days ago. Wounds haven't closed.";
            if (daysSince < 12) return "It has been quiet since the last raid. Suspiciously quiet.";
            if (daysSince < 25) return "Weeks without violence. The colonists are getting restless.";
            return "It has been a very long time since the last raid. Something must be coming.";
        }

        private static string DescribeTemperature(float celsius)
        {
            if (celsius < -30) return "lethally cold";
            if (celsius < -10) return "bitterly freezing";
            if (celsius < 5) return "cold";
            if (celsius < 18) return "cool";
            if (celsius < 28) return "pleasant";
            if (celsius < 38) return "sweltering";
            return "dangerously hot";
        }
    }
}