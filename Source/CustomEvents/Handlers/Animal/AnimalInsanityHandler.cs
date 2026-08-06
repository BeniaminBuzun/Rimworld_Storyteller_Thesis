using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using RimWorld.Planet;
using Verse;

namespace RimTelligence.CustomEvents.Handlers.Animal
{
    public class AnimalInsanityHandler : IEffectHandler
    {
        public string EffectName => "AnimalInsanity";

        public string NarrativeDescription => $"An event where an animal goes manhunter and attacks colonists.";

        public string PromptStructure => "- type = AnimalInsanity:\r\n    defName = \"PawnKindDef name\" (string)\r\n    count = integer (number of animals)\r\n    defDescription = \"\"\r\n    value = NONE\r\n    targetPawn = \"\"";

        public string AditionalPromptStructureInfo => "select only animals from [WILDLIFE] list";

        public float BaseWeight => 6f;

        public EventAlignment Alignment => EventAlignment.Bad;

        public GlobalTargetInfo Execute(GameEffect effect, Map map)
        {
            List<Pawn> candidates = map.mapPawns.AllPawnsSpawned
                .Where(pawn => pawn.RaceProps.Animal && !pawn.InMentalState)
                .ToList();

            
            candidates = candidates.Where(pawn => pawn.kindDef.defName == effect.defName).ToList();
            

            if (!candidates.Any())
            {
                ModLogger.Warning($"AnimalInsanity: no valid animals found for '{effect.defName}'.");
                return GlobalTargetInfo.Invalid;
            }

            int count = effect.count > 0 ? Math.Min(effect.count, candidates.Count) : 1;
            candidates = candidates.InRandomOrder().Take(count).ToList();

            foreach (Pawn animal in candidates)
            {
                animal.mindState.mentalStateHandler.TryStartMentalState(MentalStateDefOf.Manhunter, forceWake: true);
                ModLogger.Message($"Drove {animal.LabelShort} manhunter.");
            }

            return new GlobalTargetInfo(candidates.First());
        }

        public string GetSummary(GameEffect effect) =>
            $"{effect.count} animal(s) {effect.defName} went manhunter";
    }
}