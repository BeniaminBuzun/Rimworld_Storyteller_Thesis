using RimTelligence.Source.DataSnapshots;
using RimWorld;
using RimWorld.Planet;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;

namespace RimTelligence.Source.CustomEvents.Handlers
{
    public class AddDiseaseHandler : IEffectHandler
    {
        public string EffectType => "DiseasePawn";
        public string PromptDescription => "- DiseasePawn        : defName = HediffDef (e.g. Malaria, Plague), targetPawn = colonist name";

        private string specialSummary; // semi hack it works different everywhere
        public string AditionalPromptInfo => $"select only from these disease {string.Join(", ", DataList.DiseaseList)}";

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

            Pawn named = availablePawns.FirstOrDefault(p => p.LabelShort.Equals(effect.targetPawn, StringComparison.OrdinalIgnoreCase));
            if (named != null) targets.Add(named);
            

            if (!targets.Any())
            {
                ModLogger.Warning($"Disease: no spawned targets found for '{effect.targetPawn}'.");
                return GlobalTargetInfo.Invalid;
            }

            foreach (Pawn pawn in targets)
            {
                Hediff existingDisease = pawn.health.hediffSet.GetFirstHediffOfDef(hediffDef);
                
                if (existingDisease != null)
                {
                    float severity = Rand.Range(0.02f, 0.1f);
                    specialSummary = $"{pawn.LabelShort} disease {hediffDef.defName} severity increased by {severity}";
                    existingDisease.Severity += severity;

                    pawn.health.Notify_HediffChanged(existingDisease);
                }
                else
                {
                    Hediff newDisease = HediffMaker.MakeHediff(hediffDef, pawn);
                    newDisease.Severity = 0.01f;
                    specialSummary = $"{pawn.LabelShort} got infected with {hediffDef.defName}";
                    pawn.health.AddHediff(newDisease);

                }

                ModLogger.Message(specialSummary);
            }

            return new GlobalTargetInfo(targets.First());
        }

        public string GetSummary(LlmEffect effect) =>
            specialSummary;
    }
}