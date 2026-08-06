using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using RimWorld;
using RimWorld.Planet;
using Verse;

namespace RimTelligence.CustomEvents.Handlers.Animal
{
    public class SelfTameHandler : IEffectHandler
    {
        public string EffectName => "SelfTame";

        public string NarrativeDescription => $"An event where an animal spontaneously tames itself.";

        public string PromptStructure => "- type = SelfTame:\r\n    defName = \"PawnKindDef name\" (string)\r\n    count = integer (number of animals)\r\n    defDescription = \"\"\r\n    value = NONE\r\n    targetPawn = \"\"";

        public string AditionalPromptStructureInfo => "select only animals from [WILDLIFE] list";

        public float BaseWeight => 6f;

        public EventAlignment Alignment => EventAlignment.Good;

        public GlobalTargetInfo Execute(GameEffect effect, Map map)
        {
            List<Pawn> candidates = map.mapPawns.AllPawnsSpawned
                .Where(pawn => pawn.RaceProps.Animal && pawn.Faction == null && !pawn.InMentalState)
                .ToList();

            candidates = candidates.Where(pawn => pawn.kindDef.defName == effect.defName).ToList();
            
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

        public string GetSummary(GameEffect effect) =>
            $"{effect.count} animal(s) {effect.defName} self-tamed";
    }
}