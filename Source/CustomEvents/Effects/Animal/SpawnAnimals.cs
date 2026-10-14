using RimTelligence.DataSnapshots;
using RimWorld.Planet;
using System;
using Verse;

namespace RimTelligence.CustomEvents.Handlers.Animal
{
    public class SpawnAnimals : IEffect
    {
        private static readonly Random _random = new Random();
        public EffectConfig Config => EffectConfigRegistry.Get("SpawnAnimals");

        public bool CanBeChoosen(ColonySnapshot snapshot) => true;

        public GlobalTargetInfo Execute(GameEffect effect, Map map)
        {
            PawnKindDef animalKind = effect.SelectedDefName != null && effect.SelectedDefName.Length > 0
                ? DefDatabase<PawnKindDef>.GetNamed(effect.SelectedDefName[0], errorOnFail: false)
                : null;
            if (animalKind == null)
            {
                ModLogger.Warning($"SpawnAnimals: PawnKindDef {effect.DefNameText} not found.");
                return GlobalTargetInfo.Invalid;
            }

            int count = CalculateAnimalCount(effect.Severity);
            IntVec3 spawnCell = CellFinder.RandomEdgeCell(map);
            Pawn lastAnimal = null;

            for (int index = 0; index < count; index++)
            {
                PawnGenerationRequest request = new PawnGenerationRequest(animalKind, null);
                Pawn animal = PawnGenerator.GeneratePawn(request);
                GenSpawn.Spawn(animal, spawnCell, map);
                lastAnimal = animal;
                ModLogger.Message($"Spawned {effect.DefNameText} at {spawnCell}.");
            }

            return lastAnimal != null ? new GlobalTargetInfo(lastAnimal) : GlobalTargetInfo.Invalid;
        }

        private int CalculateAnimalCount(SeverityTypes severity)
        {
            switch (severity)
            {
                case SeverityTypes.Low: return 1;
                case SeverityTypes.Medium: return _random.Next(2, 4);
                case SeverityTypes.High: return _random.Next(4, 6);
                case SeverityTypes.Extreme: return 7;
                default: return 0;
            }
        }

        public string GetSummary(GameEffect effect) => $"{effect.DefNameText}(s) crawled from the ground";
    }
}