using RimTelligence.DataSnapshots;
using RimWorld;
using RimWorld.Planet;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using Verse;

namespace RimTelligence.CustomEvents.Handlers.Animal
{
    public class SelfTame : IEffect
    {
        public EffectConfig Config => EffectConfigRegistry.Get("SelfTame");

        public bool CanBeChoosen(ColonySnapshot snapshot) => snapshot.wildAnimals.Any();


        public GlobalTargetInfo Execute(GameEffect effect, Map map)
        {
            List<Pawn> candidates = map.mapPawns.AllPawnsSpawned
                .Where(pawn => pawn.RaceProps.Animal && pawn.Faction == null && !pawn.InMentalState)
                .ToList();

            candidates = candidates.Where(pawn => pawn.kindDef.defName == effect.SelectedDefName[0]).ToList();
            
            if (!candidates.Any())
            {
                ModLogger.Warning($"SelfTame: no tameable animals found for '{effect.DefNameText}'.");
                return GlobalTargetInfo.Invalid;
            }

            int count = CalculateAnimalCount(candidates.Count, effect.Severity);
            candidates = candidates.InRandomOrder().Take(count).ToList();

            foreach (Pawn animal in candidates)
            {
                animal.SetFaction(Faction.OfPlayer);
                Messages.Message($"{animal.LabelShort} has spontaneously tamed itself!", animal, MessageTypeDefOf.PositiveEvent);
                ModLogger.Message($"Self-tamed {animal.LabelShort}.");
            }

            return new GlobalTargetInfo(candidates.First());
        }

        private int CalculateAnimalCount(int candidates, SeverityTypes severity)
        {
            switch (severity)
            {
                case SeverityTypes.Low: return (int)Math.Ceiling(candidates * 0.25);
                case SeverityTypes.Medium: return (int)Math.Ceiling(candidates * 0.5);
                case SeverityTypes.High: return (int)Math.Ceiling(candidates * 0.75);
                case SeverityTypes.Extreme: return candidates;
                default: return 0;
            }
        }

        public string GetSummary(GameEffect effect) =>
            $"Nerby animal(s) {effect.DefNameText} self-tamed";
    }
}