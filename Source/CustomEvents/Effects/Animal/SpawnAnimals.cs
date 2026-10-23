using RimTelligence.DataSnapshots;
using RimWorld.Planet;
using System;
using Verse;

namespace RimTelligence.CustomEvents.Handlers.Animal
{
    public class SpawnAnimals : IEffect
    {
        private static readonly Random _random = new Random();
        private readonly EffectConfig _config = EffectConfigRegistry.Get("SpawnAnimals");

        public EffectConfig Config => _config;

        private string executionSummary;

        public bool CanBeChoosen(ColonySnapshot snapshot) => true;

        public GlobalTargetInfo Execute(GameEffect effect, Map map)
        {
            PawnKindDef animalKind = effect.SelectedDefName != null && effect.SelectedDefName.Length > 0
                ? DefDatabase<PawnKindDef>.GetNamed(effect.SelectedDefName[0], errorOnFail: false)
                : null;
            if (animalKind == null)
            {
                ModLogger.Warning($"SpawnAnimals: PawnKindDef {effect.DefNameText} not found.");
                executionSummary = $"Wild beasts were sighted nearby, but vanished before entering the area.";
                return GlobalTargetInfo.Invalid;
            }

            int count = Math.Max(1, CalculateAnimalCount(effect.Severity));
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

            string animalLabel = animalKind.LabelCap.Resolve();
            executionSummary = count == 1
                ? $"A wild {animalLabel} wandered into the local area."
                : $"A group of {count} {animalLabel}s wandered into the local area.";

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
                default: return _random.Next(1, 3);
            }
        }

        public string GetSummary(GameEffect effect)
        {
            if (!string.IsNullOrEmpty(executionSummary))
                return executionSummary;

            return GetDefaultSummary(effect);
        }

        private static string GetDefaultSummary(GameEffect effect)
        {
            string animalLabel = ResolveAnimalLabel(effect);
            string sevStr = effect.Severity != SeverityTypes.None ? $" ({effect.Severity.ToString().ToLower()})" : "";
            return $"A group of {animalLabel} wanders into the region{sevStr}.";
        }

        private static string ResolveAnimalLabel(GameEffect effect)
        {
            string defName = effect.SelectedDefName != null && effect.SelectedDefName.Length > 0 ? effect.SelectedDefName[0] : effect.DefNameText;
            if (string.IsNullOrWhiteSpace(defName)) return "wild animals";
            PawnKindDef kind = DefDatabase<PawnKindDef>.GetNamed(defName, errorOnFail: false);
            return kind != null ? kind.LabelCap.Resolve() : defName;
        }
    }
}