using RimTelligence.DataSnapshots;
using RimWorld;
using RimWorld.Planet;
using System;
using System.Collections.Generic;
using System.Linq;
using Verse;

namespace RimTelligence.CustomEvents.Handlers.Animal
{
    public class SelfTame : IEffect
    {
        public EffectConfig Config => EffectConfigRegistry.Get("SelfTame");

        public bool CanBeChoosen(ColonySnapshot snapshot) => snapshot?.wildAnimals != null && snapshot.wildAnimals.Any();


        private string executionSummary;

        public GlobalTargetInfo Execute(GameEffect effect, Map map)
        {
            List<Pawn> candidates = map.mapPawns.AllPawnsSpawned
                .Where(pawn => pawn.RaceProps.Animal && pawn.Faction == null && !pawn.InMentalState)
                .ToList();

            string requestedDef = effect.SelectedDefName != null && effect.SelectedDefName.Length > 0 ? effect.SelectedDefName[0] : null;
            if (!string.IsNullOrEmpty(requestedDef))
            {
                candidates = candidates.Where(pawn => pawn.kindDef.defName == requestedDef).ToList();
            }

            if (!candidates.Any())
            {
                ModLogger.Warning($"SelfTame: no tameable animals found for '{effect.DefNameText}'.");
                executionSummary = $"A gentle aura swept over the wilds, but no {ResolveAnimalLabel(effect)} were found to be tamed.";
                return GlobalTargetInfo.Invalid;
            }

            int count = Math.Max(1, CalculateAnimalCount(candidates.Count, effect.Severity));
            count = Math.Min(count, candidates.Count);
            candidates = candidates.InRandomOrder().Take(count).ToList();

            string animalLabel = candidates.First().kindDef.LabelCap.Resolve();

            foreach (Pawn animal in candidates)
            {
                animal.SetFaction(Faction.OfPlayer);
                Messages.Message($"{animal.LabelShort} has spontaneously tamed itself!", animal, MessageTypeDefOf.PositiveEvent);
                ModLogger.Message($"Self-tamed {animal.LabelShort}.");
            }

            executionSummary = count == 1
                ? $"A wild {animalLabel} ({candidates.First().LabelShort}) felt an unexpected bond and peacefully joined the colony!"
                : $"{count} wild {animalLabel}s felt an unexpected bond and peacefully joined the colony!";

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
                default: return (int)Math.Ceiling(candidates * 0.25);
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
            return $"A wild {animalLabel} feels drawn to the settlement and peacefully joins the colony{sevStr}.";
        }

        private static string ResolveAnimalLabel(GameEffect effect)
        {
            string defName = effect.SelectedDefName != null && effect.SelectedDefName.Length > 0 ? effect.SelectedDefName[0] : effect.DefNameText;
            if (string.IsNullOrWhiteSpace(defName)) return "wild animal";
            PawnKindDef kind = DefDatabase<PawnKindDef>.GetNamed(defName, errorOnFail: false);
            return kind != null ? kind.LabelCap.Resolve() : defName;
        }
    }
}