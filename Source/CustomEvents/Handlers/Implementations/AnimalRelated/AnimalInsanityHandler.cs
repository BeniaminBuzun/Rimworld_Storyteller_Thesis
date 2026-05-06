using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using RimWorld.Planet;
using Verse;

namespace RimTelligence.Source.CustomEvents.Handlers
{
    public class AnimalInsanityHandler : IEffectHandler
    {
        public string EffectType => "AnimalInsanity";
        public string PromptDescription => "- AnimalInsanity : defName = PawnKindDef of animal, count = how many go manhunter";

        public string AditionalPromptInfo => "select only animals from [WILDLIFE] list";

        public GlobalTargetInfo Execute(LlmEffect effect, Map map)
        {
            List<Pawn> candidates = map.mapPawns.AllPawnsSpawned
                .Where(p => p.RaceProps.Animal && !p.InMentalState)
                .ToList();

            
            candidates = candidates.Where(p => p.kindDef.defName == effect.defName).ToList();
            

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

        public string GetSummary(LlmEffect effect) =>
            $"{effect.count} animal(s) {effect.defName} went manhunter";
    }
}