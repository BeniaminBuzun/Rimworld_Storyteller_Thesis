using RimTelligence.DataSnapshots;
using RimWorld.Planet;
using System;
using System.Linq;
using UnityEngine;
using Verse;

namespace RimTelligence.CustomEvents.Handlers.Pawns.Debuffs
{
    public class AddDisease : IEffect
    {
        private readonly EffectConfig _config = EffectConfigRegistry.Get("AddDisease");

        public EffectConfig Config => _config;
        private string specialSummary;

        public bool CanBeChoosen(ColonySnapshot snapshot) => true;

        public GlobalTargetInfo Execute(GameEffect effect, Map map)
        {
            string diseaseDefName = effect.SelectedDefName != null && effect.SelectedDefName.Length > 0 && !string.IsNullOrWhiteSpace(effect.SelectedDefName[0])
                ? effect.SelectedDefName[0]
                : (!string.IsNullOrWhiteSpace(effect.DefNameText) ? effect.DefNameText : null);

            HediffDef hediffDef = !string.IsNullOrEmpty(diseaseDefName)
                ? DefDatabase<HediffDef>.GetNamed(diseaseDefName, errorOnFail: false)
                : null;

            if (hediffDef == null)
            {
                hediffDef = DefDatabase<HediffDef>.GetNamed("Flu", errorOnFail: false)
                    ?? DefDatabase<HediffDef>.GetNamed("WoundInfection", errorOnFail: false);
                if (hediffDef == null)
                {
                    ModLogger.Warning($"AddDisease: HediffDef '{effect.DefNameText}' not found and fallback unavailable.");
                    specialSummary = GetDefaultSummary(effect);
                    return GlobalTargetInfo.Invalid;
                }
                ModLogger.Warning($"AddDisease: Unknown disease def '{diseaseDefName}', defaulted to '{hediffDef.defName}'.");
            }

            var allSpawnedColonists = map.mapPawns.FreeColonistsSpawned.ToList();
            if (allSpawnedColonists.Count == 0)
            {
                ModLogger.Warning("AddDisease: No spawned free colonists found on map.");
                specialSummary = GetDefaultSummary(effect);
                return GlobalTargetInfo.Invalid;
            }

            Pawn target = null;
            if (!string.IsNullOrWhiteSpace(effect.TargetPawn) && !effect.TargetPawn.Equals("random", StringComparison.OrdinalIgnoreCase))
            {
                target = allSpawnedColonists.FirstOrDefault(p =>
                    p.LabelShort.Equals(effect.TargetPawn, StringComparison.OrdinalIgnoreCase));
            }

            if (target == null)
            {
                target = allSpawnedColonists.RandomElement();
                ModLogger.Message($"AddDisease: Target colonist '{effect.TargetPawn}' not found; selected '{target.LabelShort}'.");
            }

            string diseaseLabel = !string.IsNullOrEmpty(hediffDef.label) ? hediffDef.label : hediffDef.defName;
            Hediff existing = target.health.hediffSet.GetFirstHediffOfDef(hediffDef);

            if (existing != null)
            {
                float increase = ResolveSeverityIncrease(effect.Severity);
                existing.Severity = Mathf.Clamp01(existing.Severity + increase);
                target.health.Notify_HediffChanged(existing);

                specialSummary = $"{target.LabelShort}'s condition ({diseaseLabel}) worsened significantly (+{increase:P0} severity).";
            }
            else
            {
                Hediff newDisease = HediffMaker.MakeHediff(hediffDef, target);
                float initialSeverity = ResolveInitialSeverity(hediffDef, effect.Severity);
                newDisease.Severity = initialSeverity;
                target.health.AddHediff(newDisease);

                string severityDesc = $" ({effect.Severity.ToString().ToLower()})";
                specialSummary = $"{target.LabelShort} has contracted {diseaseLabel}{severityDesc}. Medical attention is advised.";
            }

            ModLogger.Message($"AddDisease: {specialSummary}");
            return new GlobalTargetInfo(target);
        }

        private static float ResolveInitialSeverity(HediffDef def, SeverityTypes severity)
        {
            float baseSev = def.initialSeverity > 0.05f ? def.initialSeverity : 0.20f;
            switch (severity)
            {
                case SeverityTypes.Low: return Mathf.Clamp(baseSev * 0.75f, 0.10f, 0.25f);
                case SeverityTypes.Medium: return Mathf.Clamp(baseSev * 1.0f, 0.20f, 0.40f);
                case SeverityTypes.High: return Mathf.Clamp(baseSev * 1.4f, 0.35f, 0.60f);
                case SeverityTypes.Extreme: return Mathf.Clamp(baseSev * 1.8f, 0.50f, 0.80f);
                default: return baseSev;
            }
        }

        private static float ResolveSeverityIncrease(SeverityTypes severity)
        {
            switch (severity)
            {
                case SeverityTypes.Low: return Rand.Range(0.05f, 0.10f);
                case SeverityTypes.Medium: return Rand.Range(0.10f, 0.20f);
                case SeverityTypes.High: return Rand.Range(0.20f, 0.35f);
                case SeverityTypes.Extreme: return Rand.Range(0.35f, 0.50f);
                default: return Rand.Range(0.08f, 0.15f);
            }
        }

        public string GetSummary(GameEffect effect)
        {
            if (!string.IsNullOrEmpty(specialSummary))
                return specialSummary;

            return GetDefaultSummary(effect);
        }

        private static string GetDefaultSummary(GameEffect effect)
        {
            string disease = effect?.SelectedDefName != null && effect.SelectedDefName.Length > 0 && !string.IsNullOrWhiteSpace(effect.SelectedDefName[0])
                ? effect.SelectedDefName[0]
                : (!string.IsNullOrWhiteSpace(effect?.DefNameText) ? effect.DefNameText : "an illness");

            HediffDef def = DefDatabase<HediffDef>.GetNamed(disease, errorOnFail: false);
            string diseaseLabel = def != null ? (!string.IsNullOrEmpty(def.label) ? def.label : def.defName) : disease;
            string pawn = !string.IsNullOrWhiteSpace(effect?.TargetPawn) ? effect.TargetPawn : "A colonist";
            string sevStr = effect != null ? $" ({effect.Severity.ToString().ToLower()})" : "";
            return $"{pawn} has contracted {diseaseLabel}{sevStr}. Medical treatment is advised.";
        }
    }
}