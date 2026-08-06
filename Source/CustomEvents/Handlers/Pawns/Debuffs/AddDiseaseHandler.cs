using RimTelligence.DataSnapshots;
using RimWorld;
using RimWorld.Planet;
using System;
using System.Collections.Generic;
using System.Linq;
using Verse;

namespace RimTelligence.CustomEvents.Handlers.Pawns.Debuffs
{
    public class AddDiseaseHandler : IEffectHandler
    {
        public string EffectName => "DiseasePawn";

        public string NarrativeDescription => "An event where a specific colonist contracts a disease.";

        public string PromptStructure =>
            "- type = DiseasePawn:\r\n" +
            "    defName = \"HediffDef name\" (e.g. Malaria, Plague, Flu)\r\n" +
            "    count = 0\r\n" +
            "    defDescription = \"\"\r\n" +
            "    value = NONE\r\n" +
            "    targetPawn = \"colonist name\"";

        public string AditionalPromptStructureInfo =>
            $"select only from  {string.Join(", ", GameObjectsCapturer.DiseaseList.Select(entry => $"{entry.DefName}: {entry.Description}"))}";

        public float BaseWeight => 3f;

        public EventAlignment Alignment => EventAlignment.Bad;

        private string specialSummary;

        public GlobalTargetInfo Execute(GameEffect effect, Map map)
        {
            HediffDef hediffDef = DefDatabase<HediffDef>.GetNamed(effect.defName, errorOnFail: false);
            if (hediffDef == null)
            {
                ModLogger.Warning($"DiseasePawn: HediffDef '{effect.defName}' not found.");
                return GlobalTargetInfo.Invalid;
            }

            Pawn target = map.mapPawns.FreeColonistsSpawned
                .FirstOrDefault(pawn => pawn.LabelShort.Equals(effect.targetPawn, StringComparison.OrdinalIgnoreCase));

            if (target == null)
            {
                ModLogger.Warning($"DiseasePawn: no spawned colonist found for '{effect.targetPawn}'.");
                return GlobalTargetInfo.Invalid;
            }

            Hediff existing = target.health.hediffSet.GetFirstHediffOfDef(hediffDef);
            if (existing != null)
            {
                float severity = Rand.Range(0.02f, 0.1f);
                existing.Severity += severity;
                target.health.Notify_HediffChanged(existing);
                specialSummary = $"{target.LabelShort}'s {hediffDef.defName} severity increased by {severity:F2}";
            }
            else
            {
                Hediff newDisease = HediffMaker.MakeHediff(hediffDef, target);
                newDisease.Severity = 0.01f;
                target.health.AddHediff(newDisease);
                specialSummary = $"{target.LabelShort} contracted {hediffDef.defName}";
            }

            ModLogger.Message($"DiseasePawn: {specialSummary}.");
            return new GlobalTargetInfo(target);
        }

        public string GetSummary(GameEffect effect) => specialSummary;
    }
}