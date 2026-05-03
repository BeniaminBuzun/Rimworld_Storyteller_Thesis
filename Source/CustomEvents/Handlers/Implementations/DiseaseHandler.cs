using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using RimWorld.Planet;
using Verse;

namespace RimTelligence.Source.CustomEvents.Handlers
{
    public class DiseaseHandler : IEffectHandler
    {
        public string EffectType => "Disease";
        public string PromptDescription => "- Disease        : defName = HediffDef (e.g. Malaria, Plague), value = severity 0.0-1.0, targetPawn = colonist name or \"random\" or \"all\"";

        public string AditionalPromptInfo => "select only from these disease types (Flu, Plague, Malaria, SleepingSickness, GutWorms, MuscleParasites, SensoryMechanites, FibrousMechanites, FoodPoisoning, ToxicBuildup, BloodRot, LungRot, Scaria, Cataract, Frail, BadBack)";

        public GlobalTargetInfo Execute(LlmEffect effect, Map map)
        {
            HediffDef hediffDef = DefDatabase<HediffDef>.GetNamed(effect.defName, errorOnFail: false);
            if (hediffDef == null)
            {
                ModLogger.Warning($"Disease: HediffDef '{effect.defName}' not found.");
                return GlobalTargetInfo.Invalid;
            }

            var availablePawns = map.mapPawns.FreeColonistsSpawned;
            List<Pawn> targets = new List<Pawn>();

            if (effect.targetPawn.Equals("all", StringComparison.OrdinalIgnoreCase))
            {
                targets = availablePawns.ToList();
            }
            else if (effect.targetPawn.Equals("random", StringComparison.OrdinalIgnoreCase))
            {
                Pawn random = availablePawns.RandomElementWithFallback();
                if (random != null) targets.Add(random);
            }
            else
            {
                Pawn named = availablePawns.FirstOrDefault(p => p.LabelShort.Equals(effect.targetPawn, StringComparison.OrdinalIgnoreCase));
                if (named != null) targets.Add(named);
            }

            if (!targets.Any())
            {
                ModLogger.Warning($"Disease: no spawned targets found for '{effect.targetPawn}'.");
                return GlobalTargetInfo.Invalid;
            }

            foreach (Pawn pawn in targets)
            {
                Hediff hediff = HediffMaker.MakeHediff(hediffDef, pawn);

                if (effect.value > 0)
                {
                    hediff.Severity = effect.value;
                }

                pawn.health.AddHediff(hediff);
                ModLogger.Message($"Applied {hediffDef.defName} (severity: {hediff.Severity:F2}) to {pawn.LabelShort}");
            }

            return new GlobalTargetInfo(targets.First());
        }

        public string GetSummary(LlmEffect effect) =>
            $"{effect.defName} (severity {effect.value:F2}) afflicted {(effect.targetPawn == "all" ? "all colonists" : effect.targetPawn == "random" ? "a random colonist" : effect.targetPawn)}";
    }
}