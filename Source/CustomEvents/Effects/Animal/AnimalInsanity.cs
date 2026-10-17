using RimTelligence.DataSnapshots;
using RimWorld;
using RimWorld.Planet;
using System;
using System.Collections.Generic;
using System.Linq;
using Verse;

namespace RimTelligence.CustomEvents.Handlers.Animal
{
    public class AnimalInsanity : IEffect
    {
        public EffectConfig Config => EffectConfigRegistry.Get("AnimalInsanity");

        public bool CanBeChoosen(ColonySnapshot snapshot) => snapshot?.wildAnimals != null && snapshot.wildAnimals.Any();

        private string executionSummary;

        public GlobalTargetInfo Execute(GameEffect effect, Map map)
        {
            List<Pawn> candidates = map.mapPawns.AllPawnsSpawned
                .Where(pawn => pawn.RaceProps.Animal && !pawn.InMentalState)
                .ToList();

            string requestedDef = effect.SelectedDefName != null && effect.SelectedDefName.Length > 0 ? effect.SelectedDefName[0] : null;
            if (!string.IsNullOrEmpty(requestedDef))
            {
                candidates = candidates.Where(pawn => pawn.kindDef.defName == requestedDef).ToList();
            }

            if (!candidates.Any())
            {
                ModLogger.Warning($"AnimalInsanity: no valid animals found for '{effect.DefNameText}'.");
                executionSummary = $"A psychic wave echoed, but no {ResolveAnimalLabel(effect)} were present to be enraged.";
                return GlobalTargetInfo.Invalid;
            }

            int count = Math.Max(1, CalculateAnimalCount(candidates.Count, effect.Severity));
            count = Math.Min(count, candidates.Count);
            candidates = candidates.InRandomOrder().Take(count).ToList();

            string animalLabel = candidates.First().kindDef.LabelCap.Resolve();

            foreach (Pawn animal in candidates)
            {
                animal.mindState.mentalStateHandler.TryStartMentalState(MentalStateDefOf.Manhunter, forceWake: true);
                ModLogger.Message($"Drove {animal.LabelShort} manhunter.");
            }

            executionSummary = count == 1
                ? $"A psychic hum drove 1 {animalLabel} into a blind, manhunter rage!"
                : $"A psychic wave drove {count} {animalLabel}s into a furious manhunter rage!";

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
            return $"A psychic pulse drives local {animalLabel} into an uncontrollable manhunter frenzy{sevStr}.";
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