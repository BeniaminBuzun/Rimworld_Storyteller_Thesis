using RimTelligence.Source.CustomEvents;
using RimTelligence.Source.DataSnapshots.Capture;
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
                colonists = PawnSnapshotCapturer.CaptureColonists(map.mapPawns.FreeColonists.ToList()),
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
                                    .GroupBy(animal => animal.def.defName)
                                    .ToDictionary(group => group.Key, group => group.Count()),

                cultivatedPlants = map.listerThings
                    .ThingsInGroup(ThingRequestGroup.Plant)
                    .OfType<Plant>()
                    .Where(p => p.DeliberatelyCultivated())
                    .GroupBy(p => p.def.defName)
                    .ToDictionary(g => g.Key, g => g.Count()),

                resources = CaptureResources(map),
                recentEventTitles = GetLastEvents(),

            };
        }

        //Raid history
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


    private static List<string> GetLastEvents()
        {
            var titles = new List<string>();
            try
            {
                var archivables = Find.Archive.ArchivablesListForReading;

                for (int i = archivables.Count - 1; i >= 0; i--)
                {
                    if (archivables[i] is Letter letter)
                    {
                        titles.Add(letter.Label);
                        if (titles.Count >= 15) break;
                    }
                }
            }
            catch (Exception ex)
            {
                ModLogger.Error($"Failed to fetch recent events: {ex.Message}");
            }
            return titles;
        }
    }
}