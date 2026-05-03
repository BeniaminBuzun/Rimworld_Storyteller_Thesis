using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using Verse;

namespace RimTelligence.Source.DataSnapshots
{
    public static class ColonySnapshotCapturer
    {
        public static ColonySnapshot Capture(Map map)
        {
            return new ColonySnapshot
            {
                colonyName = Find.FactionManager.OfPlayer.Name,
                day = GenLocalDate.DayOfYear(map),
                time = GenLocalDate.HourOfDay(map).ToString("00"),
                season = GenLocalDate.Season(map).ToString(),
                biome = map.Biome.defName,
                colonistCount = map.mapPawns.FreeColonistsCount,
                colonists = map.mapPawns.FreeColonists
                                     .Select(CaptureColonist)
                                     .ToList(),
                wealthTotal = map.wealthWatcher.WealthTotal,
                wealthItems = map.wealthWatcher.WealthItems,
                wealthBuildings = map.wealthWatcher.WealthBuildings,
                wealthPawns = map.wealthWatcher.WealthPawns,

                daysSinceLastRaid = CaptureDaysSinceLastRaid(map),

                activeConditions = map.gameConditionManager.ActiveConditions
                                         .Select(c => c.def.defName)
                                         .ToList(),

                totalNutrition = map.resourceCounter.TotalHumanEdibleNutrition,

                currentWeather = map.weatherManager.curWeather.defName,

                temperature = map.mapTemperature.OutdoorTemp,

                wildAnimals = map.mapPawns.AllPawnsSpawned
                                    .Where(animal => animal.RaceProps.Animal && animal.Faction == null && !animal.InMentalState)
                                    .Select(animal => animal.def.defName)
                                    .Distinct()
                                    .ToList(),

                resources = CaptureResources(map),

                recentIncidentsTitles = GetRecentRandomIncidents(map), // may not work need testing does not add to list when dev fired 
                recentRandomQuestsTitles = GetRecentRandomQuests(map), // may not work need testing
                recentRandomDecreesTitles = GetRecentRandomDecrees(map) // may not work need testing
            };
        }

        //Pawn
        private static PawnSnapshot CaptureColonist(Pawn pawn)
        {
            return new PawnSnapshot
            {
                name = pawn.Name?.ToStringShort ?? "Unknown",
                age = pawn.ageTracker?.AgeBiologicalYears ?? 0,
                mood = pawn.needs?.mood?.CurLevelPercentage ?? 0f,
                health = pawn.health?.summaryHealth?.SummaryHealthPercent ?? 1f,
                traits = CaptureTraits(pawn),
                childhood = pawn.story?.Childhood?.title ?? "Unknown",
                adulthood = pawn.story?.Adulthood?.title ?? "None",
                skills = CaptureSkills(pawn),
                hediffs = CaptureHediffs(pawn) // any body modifires 
            };
        }

        private static List<string> CaptureTraits(Pawn pawn)
        {
            try
            {
                return pawn.story?.traits?.allTraits
                    ?.Select(t => t.def.defName)
                    .ToList()
                    ?? new List<string>();
            }
            catch (Exception) { return new List<string>(); }
        }

        private static List<string> CaptureSkills(Pawn pawn)
        {
            try
            {
                return pawn.skills?.skills
                    ?.OrderByDescending(s => s.levelInt)
                    .Take(3)
                    .Select(s => $"{s.def.defName}:{s.levelInt}")
                    .ToList()
                    ?? new List<string>();
            }
            catch (Exception) { return new List<string>(); }
        }

        private static List<string> CaptureHediffs(Pawn pawn)
        {
            try
            {
                return pawn.health?.hediffSet?.hediffs
                    .Where(h => h.Severity > 0.01f) // skip near-zero hediffs
                    .Select(h => h.def.defName)
                    .Distinct()
                    .ToList()
                    ?? new List<string>();
            }
            catch (Exception) { return new List<string>(); }
        }

        // ── Raid history ─────────────────────────────────────────────────────

        private static int CaptureDaysSinceLastRaid(Map map)
        {
            try
            {
                int lastThreatBigTick = map.StoryState.LastThreatBigTick;
                if (lastThreatBigTick <= 0) return -1;

                int elapsedTicks = Find.TickManager.TicksGame - lastThreatBigTick;
                return elapsedTicks / GenDate.TicksPerDay;
            }
            catch (Exception) { return -1; }
        }

        //Resources
        private static ResourceSnapshot CaptureResources(Map map)
        {
            var filtered = new Dictionary<string, int>();

            try
            {
                foreach (var kvp in map.resourceCounter.AllCountedAmounts)
                {
                    if (kvp.Value <= 0) continue;
                    filtered[kvp.Key.defName] = kvp.Value;
                }
            }
            catch (Exception) {}

            return new ResourceSnapshot
            {
                allResources = filtered
            };
        }

        private static List<string> GetRecentRandomIncidents(Map map) =>
            map?.StoryState?.RecentRandomIncidents?
            .Where(i => i != null)
            .Select(i => i.letterText)
            .ToList() ?? new List<string>();

        private static List<string> GetRecentRandomQuests(Map map) =>
           map?.StoryState?.RecentRandomQuests?
           .Where(i => i != null)
           .Select(i => i.questAvailableLetterLabel) // kij wie czy to zwraca to co myśle że zwraca 
           .ToList() ?? new List<string>();

        private static List<string> GetRecentRandomDecrees(Map map) =>
           map?.StoryState?.RecentRandomDecrees?
           .Where(i => i != null)
           .Select(i => i.questAvailableLetterLabel) // kij wie czy to zwraca to co myśle że zwraca 
           .ToList() ?? new List<string>();
    }
}