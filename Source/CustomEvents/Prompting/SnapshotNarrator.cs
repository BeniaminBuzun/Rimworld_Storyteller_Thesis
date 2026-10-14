using RimTelligence.DataSnapshots;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace RimTelligence.CustomEvents.Prompting
{
    public static class SnapshotNarrator
    {
        private static readonly Random _rng = new Random();

        private const int MaxResourceEntries = 10;
        private const int SpotlightCount = 2;
        private const int BriefCount = 999;

        public static string Narrate(ColonySnapshot snapshot)
        {
            if (snapshot == null)
            {
                ModLogger.Debug("SnapshotNarrator.Narrate: snapshot is null; returning neutral narrative.");
                return "A small colony on the edge of the Rimworld. Colony details are currently unavailable.";
            }

            ModLogger.Debug("SnapshotNarrator.Narrate: narrating colony '" + snapshot.colonyName + "'.");

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
                var names = snapshot.colonists.Select(pawn => pawn.name).Where(name => !string.IsNullOrWhiteSpace(name)).ToList();
                sb.AppendLine($"Colonists ({snapshot.colonistCount} total, exact names for targetPawn): {string.Join(", ", names)}.");
                sb.AppendLine();

                var ordered = snapshot.colonists.OrderByDescending(InterestScore).ToList();
                var spotlight = ordered.Take(SpotlightCount).ToList();
                var brief = ordered.Skip(SpotlightCount).ToList();

                sb.AppendLine("Key colonists (full detail):");
                foreach (var pawn in spotlight)
                    sb.AppendLine("  " + DescribePawnFull(pawn));

                if (brief.Count > 0)
                {
                    sb.AppendLine("Other colonists (brief):");
                    foreach (var pawn in brief)
                        sb.AppendLine("  " + DescribePawnBrief(pawn));
                }
                sb.AppendLine();
            }

            // Colony animals
            if (!string.IsNullOrWhiteSpace(snapshot.colonyAnimals?.ToString()))
            {
                var animalList = snapshot.colonyAnimals ?? new List<AnimalSnapshot>();
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
            if (snapshot.colonyResources?.allResources != null && snapshot.colonyResources.allResources.Count > 0)
            {
                var topResources = snapshot.colonyResources.allResources
                    .OrderByDescending(kv => kv.Value)
                    .Take(MaxResourceEntries)
                    .Select(kv => $"{kv.Key}: {kv.Value}");
                string suffix = snapshot.colonyResources.allResources.Count > MaxResourceEntries
                    ? $" (+ {snapshot.colonyResources.allResources.Count - MaxResourceEntries} more)"
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
            // [TODO] for implementation

            return sb.ToString().Trim();
        }

        private static float InterestScore(PawnSnapshot pawn)
        {
            float score = 0f;
            if (pawn.health?.is_downed == true) score += 10f;
            if (pawn.health?.diseases?.Count > 0) score += 5f;
            if (pawn.health?.injuries?.Count > 0) score += 3f;
            if (pawn.health?.missing_body_parts?.Count > 0) score += 4f;
            if (pawn.needs.mood < 0.3f) score += 4f;
            else if (pawn.needs.mood < 0.5f) score += 2f;
            if (pawn.traits?.Count > 0) score += 1f;
            return score;
        }

        private static string DescribePawnFull(PawnSnapshot pawn)
        {
            var sb = new StringBuilder();
            sb.Append($"{pawn.name} (age {pawn.age})");

            // Location & drafted status
            if (!string.IsNullOrWhiteSpace(pawn.current_location))
                sb.Append($" [LOCATION: {pawn.current_location.ToLower()}]");
            if (pawn.is_drafted)
                sb.Append(" [DRAFTED]");

            // Weapon
            if (!string.IsNullOrWhiteSpace(pawn.primary_weapon))
                sb.Append($" [WEAPON: {pawn.primary_weapon.ToLower()}]");

            // Current action & queue
            if (!string.IsNullOrWhiteSpace(pawn.current_action))
                sb.Append($" [ACTION: {pawn.current_action.ToLower()}]");
            if (pawn.queued_actions?.Count > 0)
                sb.Append($" [QUEUE: {string.Join(" → ", pawn.queued_actions).ToLower()}]");

            // Mood + other needs
            sb.Append($" [MOOD: {DescribeMood(pawn.needs.mood)}]");
            sb.Append(DescribeOtherNeeds(pawn.needs));

            // Health block
            if (pawn.health != null)
            {
                sb.Append($" [HEALTH: {pawn.health.overall_health:P0}");
                if (pawn.health.is_downed) sb.Append(", DOWNED");
                if (pawn.health.pain_level > 0.3f) sb.Append($", pain {pawn.health.pain_level:P0}");
                if (pawn.health.injuries?.Count > 0) sb.Append($", injuries: {string.Join(", ", pawn.health.injuries)}");
                if (pawn.health.diseases?.Count > 0) sb.Append($", diseases: {string.Join(", ", pawn.health.diseases)}");
                if (pawn.health.missing_body_parts?.Count > 0) sb.Append($", missing: {string.Join(", ", pawn.health.missing_body_parts)}");
                if (pawn.health.implants?.Count > 0) sb.Append($", implants: {string.Join(", ", pawn.health.implants)}");
                if (pawn.health.chronic_conditions?.Count > 0) sb.Append($", chronic: {string.Join(", ", pawn.health.chronic_conditions)}");
                sb.Append("]");
            }

            // Traits
            if (pawn.traits?.Count > 0)
                sb.Append($" [TRAITS: {string.Join(", ", pawn.traits)}]");

            // Background
            if (!string.IsNullOrWhiteSpace(pawn.childhood) || !string.IsNullOrWhiteSpace(pawn.adulthood))
                sb.Append($" [PAST: {pawn.childhood}/{pawn.adulthood}]");

            // Skills
            if (pawn.skills?.Count > 0)
                sb.Append($" [SKILLS: {string.Join(", ", pawn.skills)}]");

            // Relations
            if (pawn.relations != null)
            {
                var rel = new List<string>();
                if (pawn.relations.lovers?.Count > 0)
                    rel.Add($"lovers: {string.Join(", ", pawn.relations.lovers)}");
                if (pawn.relations.rivals?.Count > 0)
                    rel.Add($"rivals: {string.Join(", ", pawn.relations.rivals)}");
                if (!string.IsNullOrWhiteSpace(pawn.relations.bonded_animal))
                    rel.Add($"bonded animal: {pawn.relations.bonded_animal}");
                if (rel.Count > 0)
                    sb.Append($" [RELATIONS: {string.Join("; ", rel)}]");
            }

            return sb.ToString();
        }

        private static string DescribePawnBrief(PawnSnapshot pawn)
        {
            var sb = new StringBuilder();
            sb.Append($"{pawn.name}");

            if (!string.IsNullOrWhiteSpace(pawn.current_location))
                sb.Append($" [LOC: {pawn.current_location.ToLower()}]");
            if (pawn.is_drafted)
                sb.Append(" [DRAFTED]");

            if (!string.IsNullOrWhiteSpace(pawn.current_action))
                sb.Append($" [ACTION: {pawn.current_action.ToLower()}]");

            sb.Append($" [MOOD: {DescribeMood(pawn.needs.mood)}]");
            sb.Append(DescribeOtherNeedsBrief(pawn.needs));

            if (pawn.health?.is_downed == true) sb.Append(" [DOWNED]");
            if (pawn.health?.diseases?.Count > 0) sb.Append($" [SICK: {string.Join(", ", pawn.health.diseases)}]");
            if (pawn.traits?.Count > 0) sb.Append($" [TRAITS: {string.Join(", ", pawn.traits)}]");

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