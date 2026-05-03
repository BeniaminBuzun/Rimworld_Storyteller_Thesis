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

        public static string Narrate(ColonySnapshot s)
        {
            var sb = new StringBuilder();

            //Setting
            sb.Append($"The colony \"{s.colonyName}\" scratches out an existence on day {s.day}");
            sb.Append($" — {s.season?.ToLower() ?? "some godforsaken season"} in a {s.biome?.ToLower() ?? "hostile"} biome.");
            sb.Append($" Outside it is {DescribeTemperature(s.temperature)} and {s.currentWeather?.ToLower() ?? "unclear"}.");
            sb.AppendLine();

            //Population
            sb.Append($"There are {s.colonistCount} colonist(s).");
            if (s.colonists != null && s.colonists.Count > 0)
            {
                var highlights = PickInterestingPawns(s.colonists);
                foreach (var line in highlights)
                    sb.Append(" " + line);
            }
            sb.AppendLine();

            //Colonist list (names only, for targetPawn matching)
            if (s.colonists != null && s.colonists.Count > 0)
            {
                var names = s.colonists.Select(p => p.name).Where(n => !string.IsNullOrWhiteSpace(n));
                sb.AppendLine($"Colonist names (use exact spelling for targetPawn): {string.Join(", ", names)}.");
            }

            //Food pressure
            sb.AppendLine(DescribeFood(s.totalNutrition, s.colonistCount));

            //Wealth feel
            sb.AppendLine(DescribeWealth(s.wealthTotal));

            //Threats & timing
            sb.AppendLine(DescribeRaidPressure(s.daysSinceLastRaid));

            //Active conditions
            if (s.activeConditions != null && s.activeConditions.Count > 0)
                sb.AppendLine($"The colony is currently enduring: {string.Join(", ", s.activeConditions)}.");

            //Wildlife
            if (s.wildAnimals != null && s.wildAnimals.Count > 0)
            {
                var sample = s.wildAnimals.Distinct().OrderBy(_ => _rng.Next()).Take(5).ToList();
                sb.AppendLine($"Wildlife spotted nearby: {string.Join(", ", sample)}.");
            }

            //Recent events
            if (s.recentIncidentsTitles != null && s.recentIncidentsTitles.Count > 0)
                sb.AppendLine($"Recent incidents to avoid repeating: {string.Join("; ", s.recentIncidentsTitles.Take(5))}.");

            return sb.ToString().Trim();
        }

        //Helpers

        private static IEnumerable<string> PickInterestingPawns(List<PawnSnapshot> pawns)
        {
            // Pick up to 3 pawns with something worth mentioning
            var lines = new List<string>();
            var shuffled = pawns.OrderBy(_ => _rng.Next()).ToList();

            foreach (var p in shuffled.Take(3))
            {
                string line = DescribePawn(p);
                if (line != null) lines.Add(line);
            }
            return lines;
        }

        private static string DescribePawn(PawnSnapshot p)
        {
            var parts = new List<string>();

            // Mood
            if (p.mood < 0.25f) parts.Add("on the verge of a breakdown");
            else if (p.mood < 0.45f) parts.Add("miserable");
            else if (p.mood > 0.85f) parts.Add("oddly euphoric");
            else if (p.mood > 0.7f) parts.Add("in good spirits");

            // Health
            if (p.health < 0.4f) parts.Add("barely holding together");
            else if (p.health < 0.7f) parts.Add("nursing injuries");

            // Hediffs
            if (p.hediffs != null && p.hediffs.Count > 0)
            {
                var notable = p.hediffs.Where(h => !h.Contains("Scar") && !h.Contains("Missing")).Take(2).ToList();
                if (notable.Count > 0)
                    parts.Add($"suffering from {string.Join(" and ", notable).ToLower()}");
            }

            // Traits (pick one interesting one)
            if (p.traits != null && p.traits.Count > 0)
            {
                string trait = p.traits[_rng.Next(p.traits.Count)];
                parts.Add($"known for being {trait.ToLower()}");
            }

            // Top skill
            if (p.skills != null && p.skills.Count > 0)
                parts.Add($"skilled in {p.skills[0].ToLower()}");

            if (parts.Count == 0) return null;
            return $"{p.name} is {string.Join(", ", parts)}.";
        }

        private static string DescribeFood(float nutrition, int colonistCount)
        {
            if (colonistCount <= 0) return "Food situation: unknown.";
            float daysLeft = nutrition / Math.Max(colonistCount * 1.6f, 1f);

            if (daysLeft < 2) return "The colony is starving. Meals are being rationed to crumbs.";
            if (daysLeft < 5) return "Food is dangerously low — hunger is a constant companion.";
            if (daysLeft < 12) return "Provisions are modest. Nobody is starving, but nobody is comfortable either.";
            if (daysLeft < 25) return "Food stocks are adequate.";
            return "The pantry is embarrassingly full. Abundance breeds complacency.";
        }

        private static string DescribeWealth(float wealth)
        {
            if (wealth < 15000) return "The colony is dirt poor — scavenged furniture and desperate hope.";
            if (wealth < 40000) return "The colony is scraping by, with enough to survive but little to spare.";
            if (wealth < 100000) return "A modest, functional colony — not impressive, but standing.";
            if (wealth < 250000) return "The colony has accumulated real wealth. Comfortable, almost.";
            return "The colony is grotesquely wealthy by rimworld standards. A tempting target.";
        }

        private static string DescribeRaidPressure(int daysSince)
        {
            if (daysSince < 0) return "Raiders have not yet come — but they will.";
            if (daysSince == 0) return "Raiders struck today. The blood is still fresh.";
            if (daysSince < 4) return "The last raid was only days ago. Wounds haven't closed.";
            if (daysSince < 12) return "It has been quiet since the last raid. Suspiciously quiet.";
            if (daysSince < 25) return "Weeks without violence. The colonists are getting restless.";
            return "It has been a long time since the last raid. Something must be coming.";
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