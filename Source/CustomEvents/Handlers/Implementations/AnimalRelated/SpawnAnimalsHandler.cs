using RimTelligence.Source.DataSnapshots;
using RimWorld;
using RimWorld.Planet;
using System;
using System.Linq;
using Verse;

namespace RimTelligence.Source.CustomEvents.Handlers
{
    public class SpawnAnimalsHandler : IEffectHandler
    {
        public string EffectName => "SpawnAnimals";

        public string NarrativeDescription => $"An event where an animals are spawned on the map.";

        public string PromptStructure => "- type = SpawnAnimals:\r\n    defName = \"PawnKindDef name\" (string)\r\n    count = integer (number of animals)\r\n    defDescription = \"\"\r\n    value = NONE\r\n    targetPawn = \"\"";

        public string AditionalPromptStructureInfo => $"select only from  {string.Join(", ", GameObjectsCapturer.AnimalList.Select(m => $"{m.DefName}: {m.Description}"))}";

        public float BaseWeight => 6f;

        public EventAlignment Alignment => EventAlignment.Neutral;

        public GlobalTargetInfo Execute(GameEffect effect, Map map)
        {
            PawnKindDef animalKind = PawnKindDef.Named(effect.defName);
            if (animalKind == null)
            {
                ModLogger.Warning($"SpawnAnimals: PawnKindDef {effect.defName} not found.");
                return GlobalTargetInfo.Invalid;
            }

            int count = effect.count > 0 ? effect.count : 3;
            IntVec3 spawnCell = CellFinder.RandomEdgeCell(map);
            Pawn lastAnimal = null;

            for (int i = 0; i < count; i++)
            {
                PawnGenerationRequest request = new PawnGenerationRequest(animalKind, null);
                Pawn animal = PawnGenerator.GeneratePawn(request);
                GenSpawn.Spawn(animal, CellFinder.RandomEdgeCell(map), map);
                lastAnimal = animal;
                ModLogger.Message($"Spawned {effect.defName} at {spawnCell}.");
            }

            return lastAnimal != null ? new GlobalTargetInfo(lastAnimal) : GlobalTargetInfo.Invalid;
        }

        public string GetSummary(GameEffect effect) => $"{effect.count} {effect.defName}(s) spawned";
    }
}