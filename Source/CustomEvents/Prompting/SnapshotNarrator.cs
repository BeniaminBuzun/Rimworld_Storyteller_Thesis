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

        public static string Narrate(ColonySnapshot snapshot)
        {
            var sb = new StringBuilder();

            // Identity
            sb.Append($"Colony \"{snapshot.colonyName}\", day {snapshot.day}");
            if (!string.IsNullOrWhiteSpace(snapshot.time))
                sb.Append($" ({snapshot.time})");
            sb.Append($" — {snapshot.season?.ToLower() ?? "unknown season"}, {snapshot.biome?.ToLower() ?? "hostile biome"}.");
            sb.Append($" Outside: {DescribeTemperature(snapshot.temperature)}, {snapshot.currentWeather?.ToLower() ?? "unclear weather"}.");
            sb.AppendLine();

            // Colonists
            if (snapshot.colonists != null && snapshot.colonists.Count > 0)
            {
                var names = snapshot.colonists.Select(p => p.name).Where(n => !string.IsNullOrWhiteSpace(n)).ToList();
                sb.AppendLine($"Colonists ({snapshot.colonistCount} total, exact names for targetPawn): {string.Join(", ", names)}.");
                sb.AppendLine();

                var ordered = snapshot.colonists.OrderByDescending(InterestScore).ToList();
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

            // Colony animals
            if (!string.IsNullOrWhiteSpace(snapshot.colonyAnimals?.ToString()))
            {
                var animalList = snapshot.colonyAnimals ?? new List<string>();
                if (animalList.Count > 0)
                    sb.AppendLine($"Colony animals ({snapshot.colonyAnimalCount}): {string.Join(", ", animalList)}.");
                else
                    sb.AppendLine($"Colony animal count: {snapshot.colonyAnimalCount} (no names recorded).");
            }

            // Nutrition / food description
            sb.AppendLine(DescribeFood(snapshot.totalNutrition, snapshot.colonistCount));

            // Wealth (detailed)
            sb.AppendLine(DescribeWealth(snapshot.wealthTotal, snapshot.wealthItems, snapshot.wealthBuildings, snapshot.wealthPawns));

            // Tech level
            if (!string.IsNullOrWhiteSpace(snapshot.currentTechLevel))
                sb.AppendLine($"Technology level: {snapshot.currentTechLevel}.");

            // Under attack
            //if (s.isUnderAttck) // note: this is the original field name; we preserve it
            //    sb.AppendLine("*** THE COLONY IS UNDER ATTACK RIGHT NOW ***");

            // Raid pressure
            sb.AppendLine(DescribeRaidPressure(snapshot.daysSinceLastRaid));

            // Active conditions
            if (snapshot.activeConditions != null && snapshot.activeConditions.Count > 0)
                sb.AppendLine($"Active conditions: {string.Join(", ", snapshot.activeConditions)}.");

            // Wild animals
            if (snapshot.wildAnimals != null && snapshot.wildAnimals.Count > 0)
            {
                var sample = snapshot.wildAnimals
                    .OrderByDescending(kv => kv.Value)
                    .Take(6)
                    .Select(kv => $"{kv.Value}x {kv.Key}")
                    .ToList();
                sb.AppendLine($"Wildlife nearby: {string.Join(", ", sample)}.");
            }

            // Cultivated plants
            if (snapshot.cultivatedPlants != null && snapshot.cultivatedPlants.Count > 0)
            {
                var crops = snapshot.cultivatedPlants
                    .Select(kv => $"{kv.Value}x {kv.Key}")
                    .ToList();
                sb.AppendLine($"Crops: {string.Join(", ", crops)}.");
            }

            // Resources
            if (snapshot.resources?.allResources != null && snapshot.resources.allResources.Count > 0)
            {
                var topResources = snapshot.resources.allResources
                    .OrderByDescending(kv => kv.Value)
                    .Take(MaxResourceEntries)
                    .Select(kv => $"{kv.Key}: {kv.Value}");
                string suffix = snapshot.resources.allResources.Count > MaxResourceEntries
                    ? $" (+ {snapshot.resources.allResources.Count - MaxResourceEntries} more)"
                    : "";
                sb.AppendLine($"Resources: {string.Join(", ", topResources)}{suffix}.");
            }

            // Faction relations
            if (snapshot.factionRelations != null && snapshot.factionRelations.Count > 0)
            {
                var relList = snapshot.factionRelations
                    .Select(kv => $"{kv.Key}: {kv.Value}")
                    .ToList();
                sb.AppendLine($"Faction relations: {string.Join(", ", relList)}.");
            }

            // Active quests
            if (snapshot.activeQuests != null && snapshot.activeQuests.Count > 0)
                sb.AppendLine($"Active quests: {string.Join(", ", snapshot.activeQuests)}.");

            // Recent events
            if (snapshot.recentEvents != null && snapshot.recentEvents.Count > 0 && _rng.NextDouble() < 0.5)
                sb.AppendLine($"[RECENT EVENTS — do not repeat]: {string.Join("; ", snapshot.recentEvents.Select(m => $"{m.title}: {m.description}"))}.");
            return sb.ToString().Trim();
        }

        private static float InterestScore(PawnSnapshot p)
        {
            float score = 0f;
            if (p.health?.is_downed == true) score += 10f;
            if (p.health?.diseases?.Count > 0) score += 5f;
            if (p.health?.injuries?.Count > 0) score += 3f;
            if (p.health?.missing_body_parts?.Count > 0) score += 4f;
            if (p.needs.mood < 0.3f) score += 4f;
            else if (p.needs.mood < 0.5f) score += 2f;
            if (p.traits?.Count > 0) score += 1f;
            return score;
        }

        private static string DescribePawnFull(PawnSnapshot p)
        {
            var sb = new StringBuilder();
            sb.Append($"{p.name} (age {p.age})");

            // Location & drafted status
            if (!string.IsNullOrWhiteSpace(p.current_location))
                sb.Append($" [LOCATION: {p.current_location.ToLower()}]");
            if (p.is_drafted)
                sb.Append(" [DRAFTED]");

            // Weapon
            if (!string.IsNullOrWhiteSpace(p.primary_weapon))
                sb.Append($" [WEAPON: {p.primary_weapon.ToLower()}]");

            // Current action & queue
            if (!string.IsNullOrWhiteSpace(p.current_action))
                sb.Append($" [ACTION: {p.current_action.ToLower()}]");
            if (p.queued_actions?.Count > 0)
                sb.Append($" [QUEUE: {string.Join(" → ", p.queued_actions).ToLower()}]");

            // Mood + other needs
            sb.Append($" [MOOD: {DescribeMood(p.needs.mood)}]");
            sb.Append(DescribeOtherNeeds(p.needs));

            // Health block
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

            // Traits
            if (p.traits?.Count > 0)
                sb.Append($" [TRAITS: {string.Join(", ", p.traits)}]");

            // Background
            if (!string.IsNullOrWhiteSpace(p.childhood) || !string.IsNullOrWhiteSpace(p.adulthood))
                sb.Append($" [PAST: {p.childhood}/{p.adulthood}]");

            // Skills
            if (p.skills?.Count > 0)
                sb.Append($" [SKILLS: {string.Join(", ", p.skills)}]");

            // Relations
            if (p.relations != null)
            {
                var rel = new List<string>();
                if (p.relations.lovers?.Count > 0)
                    rel.Add($"lovers: {string.Join(", ", p.relations.lovers)}");
                if (p.relations.rivals?.Count > 0)
                    rel.Add($"rivals: {string.Join(", ", p.relations.rivals)}");
                if (!string.IsNullOrWhiteSpace(p.relations.bonded_animal))
                    rel.Add($"bonded animal: {p.relations.bonded_animal}");
                if (rel.Count > 0)
                    sb.Append($" [RELATIONS: {string.Join("; ", rel)}]");
            }

            return sb.ToString();
        }

        private static string DescribePawnBrief(PawnSnapshot p)
        {
            var sb = new StringBuilder();
            sb.Append($"{p.name}");

            if (!string.IsNullOrWhiteSpace(p.current_location))
                sb.Append($" [LOC: {p.current_location.ToLower()}]");
            if (p.is_drafted)
                sb.Append(" [DRAFTED]");

            if (!string.IsNullOrWhiteSpace(p.current_action))
                sb.Append($" [ACTION: {p.current_action.ToLower()}]");

            sb.Append($" [MOOD: {DescribeMood(p.needs.mood)}]");
            sb.Append(DescribeOtherNeedsBrief(p.needs));

            if (p.health?.is_downed == true) sb.Append(" [DOWNED]");
            if (p.health?.diseases?.Count > 0) sb.Append($" [SICK: {string.Join(", ", p.health.diseases)}]");
            if (p.traits?.Count > 0) sb.Append($" [TRAITS: {string.Join(", ", p.traits)}]");

            return sb.ToString();
        }

        private static string DescribeOtherNeeds(PawnNeedsSnapshot needs)
        {
            if (needs == null) return "";
            var parts = new List<string>();
            if (!string.IsNullOrWhiteSpace(needs.mental_state))
                parts.Add($"mental state: {needs.mental_state.ToLower()}");
            if (needs.food_level < 0.3f)
                parts.Add($"starving ({needs.food_level:P0})");
            else if (needs.food_level < 0.7f)
                parts.Add($"hungry ({needs.food_level:P0})");
            if (needs.rest_level < 0.3f)
                parts.Add($"exhausted ({needs.rest_level:P0})");
            else if (needs.rest_level < 0.7f)
                parts.Add($"tired ({needs.rest_level:P0})");
            if (needs.recreation_level < 0.3f)
                parts.Add($"bored ({needs.recreation_level:P0})");
            if (parts.Count == 0) return "";
            return $" [NEEDS: {string.Join(", ", parts)}]";
        }

        private static string DescribeOtherNeedsBrief(PawnNeedsSnapshot needs)
        {
            if (needs == null) return "";
            var flags = new List<string>();
            if (needs.food_level < 0.3f) flags.Add("starving");
            if (needs.rest_level < 0.3f) flags.Add("exhausted");
            if (needs.recreation_level < 0.3f) flags.Add("bored");
            if (!string.IsNullOrWhiteSpace(needs.mental_state)) flags.Add(needs.mental_state.ToLower());
            if (flags.Count == 0) return "";
            return $" [{string.Join(", ", flags)}]";
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

        private static string DescribeWealth(float total, float items, float buildings, float pawns)
        {
            var sb = new StringBuilder();
            sb.Append($"Total wealth: {total:N0}.");
            sb.Append($" Breakdown — items: {items:N0}, buildings: {buildings:N0}, pawns: {pawns:N0}. ");

            if (total < 15000)
                sb.Append("The colony is dirt poor — scavenged furniture and desperate hope.");
            else if (total < 40000)
                sb.Append("Scraping by: enough to survive, little to spare.");
            else if (total < 100000)
                sb.Append("A modest, functional colony — not impressive, but standing.");
            else if (total < 250000)
                sb.Append("The colony has accumulated real wealth. Comfortable, almost.");
            else
                sb.Append("Grotesquely wealthy by rimworld standards. A tempting target.");

            return sb.ToString();
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