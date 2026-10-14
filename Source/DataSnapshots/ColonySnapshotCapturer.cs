using Newtonsoft.Json;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using Verse;

namespace RimTelligence.DataSnapshots
{
    public static class ColonySnapshotCapturer
    {
        public static ColonySnapshot Capture(Map map)
        {
            if (map == null)
            {
                ModLogger.Error("ColonySnapshotCapturer.Capture: map is null; returning null snapshot.");
                return null;
            }

            ModLogger.Debug("ColonySnapshotCapturer.Capture: capturing colony snapshot for map '" +
                map.uniqueID + "', " + map.mapPawns.FreeColonistsCount + " free colonists.");

            return new ColonySnapshot
            {
                colonyName = Find.FactionManager.OfPlayer.Name,
                day = GenLocalDate.DayOfYear(map),
                time = GenLocalDate.HourOfDay(map).ToString("00"),
                season = GenLocalDate.Season(map).ToString(),
                biome = map.Biome.defName,
                colonistCount = map.mapPawns.FreeColonistsCount,
                colonists = PawnSnapshotCapturer.CaptureColonists(map.mapPawns.FreeColonists.ToList()),
                prisoners = PawnSnapshotCapturer.CaptureColonists(map.mapPawns.PrisonersOfColony.ToList()),
                slaves = PawnSnapshotCapturer.CaptureColonists(
                    map.mapPawns.AllPawnsSpawned
                        .Where(pawn => pawn.IsSlave && pawn.Faction == Faction.OfPlayer)
                        .ToList()),
                visitors = PawnSnapshotCapturer.CaptureColonists(
                    map.mapPawns.AllPawnsSpawned
                        .Where(pawn => pawn.RaceProps.Humanlike
                                     && pawn.Faction != null
                                 && pawn.Faction != Faction.OfPlayer
                                 && pawn.guest != null
                                 && pawn.guest.GuestStatus == GuestStatus.Guest)
                        .ToList()),
                wealthTotal = map.wealthWatcher.WealthTotal,
                wealthItems = map.wealthWatcher.WealthItems,
                wealthBuildings = map.wealthWatcher.WealthBuildings,
                wealthPawns = map.wealthWatcher.WealthPawns,

                daysSinceLastRaid = CaptureDaysSinceLastRaid(map),

                activeConditions = map.gameConditionManager.ActiveConditions
                                         .Select(condition => condition.def.defName)
                                         .ToList(),

                totalNutrition = map.resourceCounter.TotalHumanEdibleNutrition,

                currentWeather = map.weatherManager.curWeather.defName,

                temperature = map.mapTemperature.OutdoorTemp,

                wildAnimals = map.mapPawns.AllPawnsSpawned
                                    .Where(animal => animal.RaceProps.Animal && animal.Faction == null && !animal.InMentalState)
                                    .GroupBy(animal => new AnimalSnapshot
                                    {
                                        name = animal.def.label,
                                        description = animal.def.description
                                    })
                                    .ToDictionary(group => group.Key, group => group.Count()),

                cultivatedPlants = map.listerThings
                    .ThingsInGroup(ThingRequestGroup.Plant)
                    .OfType<Plant>()
                    .Where(plant => plant.DeliberatelyCultivated())
                    .GroupBy(plant => new PlantSnapshot
                    {
                        name = plant.def.label,
                        description = plant.def.description
                    })
                    .ToDictionary(group => group.Key, group => group.Count()),

                colonyResources = CaptureResources(map),
                currentTechLevel = Faction.OfPlayer.def.techLevel.ToString(),
                colonyAnimalCount = map.mapPawns.SpawnedColonyAnimals.Count,
                colonyAnimals = CaptureColonyAnimals(map),

                // world & diplomacy 
                factionRelations = CaptureFactionRelations(),
                activeQuests = CaptureActiveQuests(),
                isUnderAttck = map.attackTargetsCache.TargetsHostileToColony.Count > 0

            };
        }

        private static List<AnimalSnapshot> CaptureColonyAnimals(Map map)
        {
            try
            {
                return map.mapPawns.SpawnedColonyAnimals
                    .GroupBy(animal => new AnimalSnapshot
                    {
                        name = animal.def.label,
                        description = animal.def.description
                    })
                    .Select(group => group.Key)
                    .ToList();
            }
            catch (Exception) { return new List<AnimalSnapshot>(); }
        }

        private static Dictionary<string, int> CaptureFactionRelations()
        {
            var relations = new Dictionary<string, int>();
            try
            {
                foreach (Faction faction in Find.FactionManager.AllFactionsVisible)
                {
                    if (faction.IsPlayer || faction.Hidden || faction.defeated) continue;
                    relations[faction.Name] = faction.GoodwillWith(Faction.OfPlayer);
                }
            }
            catch (Exception) { }
            return relations;
        }

        private static List<string> CaptureActiveQuests()
        {
            var quests = new List<string>();
            try
            {
                foreach (Quest quest in Find.QuestManager.QuestsListForReading)
                {
                    if (quest.State == QuestState.Ongoing)
                    {
                        quests.Add(quest.name);
                    }
                }
            }
            catch (Exception) { }
            return quests;
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
            catch (Exception) { }

            return new ResourceSnapshot
            {
                allResources = filtered
            };
        }

        private static List<EventSnapshot> GetLastEvents()
        {
            var events = new List<EventSnapshot>();
            try
            {
                var archivables = Find.Archive.ArchivablesListForReading;

                for (int index = archivables.Count - 1; index >= 0; index--)
                {
                    if (archivables[index] is Letter letter)
                    {
                        string description = "";

                        if (letter is ChoiceLetter choiceLetter)
                        {
                            description = choiceLetter.Text.Resolve();
                        }

                        events.Add(new EventSnapshot
                        {
                            title = letter.Label,
                            description = description.StripTags()
                        });
                        // limit to last 5
                        if (events.Count >= 5) break;
                    }
                }
            }
            catch (Exception ex)
            {
                ModLogger.Error($"Failed to fetch recent events: {ex.Message}");
            }
            return events;
        }
    }
}