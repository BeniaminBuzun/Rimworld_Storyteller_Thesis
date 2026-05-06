using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using RimWorld.Planet;
using Verse;

namespace RimTelligence.Source.CustomEvents.Handlers
{
    public class SelfTameHandler : IEffectHandler
    {
        public string EffectType => "SelfTame";
        public string PromptDescription => "- SelfTame       : defName = PawnKindDef of animal, count = how many self-tame";

        public string AditionalPromptInfo => "select only animals from [WILDLIFE] list";

        public GlobalTargetInfo Execute(LlmEffect effect, Map map)
        {
            List<Pawn> candidates = map.mapPawns.AllPawnsSpawned
                .Where(p => p.RaceProps.Animal && p.Faction == null && !p.InMentalState)
                .ToList();

            candidates = candidates.Where(p => p.kindDef.defName == effect.defName).ToList();
            
            if (!candidates.Any())
            {
                ModLogger.Warning($"SelfTame: no tameable animals found for '{effect.defName}'.");
                return GlobalTargetInfo.Invalid;
            }

            int count = effect.count > 0 ? Math.Min(effect.count, candidates.Count) : 1;
            candidates = candidates.InRandomOrder().Take(count).ToList();

            foreach (Pawn animal in candidates)
            {
                animal.SetFaction(Faction.OfPlayer);
                Messages.Message($"{animal.LabelShort} has spontaneously tamed itself!", animal, MessageTypeDefOf.PositiveEvent);
                ModLogger.Message($"Self-tamed {animal.LabelShort}.");
            }

            return new GlobalTargetInfo(candidates.First());
        }

        public string GetSummary(LlmEffect effect) =>
            $"{effect.count} animal(s) {effect.defName} self-tamed";
    }
}