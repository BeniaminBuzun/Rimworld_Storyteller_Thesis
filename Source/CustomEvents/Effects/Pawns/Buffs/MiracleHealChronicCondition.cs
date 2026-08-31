using RimTelligence.CustomEvents;
using RimTelligence.CustomEvents.Handlers;
using RimTelligence.DataSnapshots;
using RimWorld.Planet;
using System;
using System.Linq;
using Verse;

namespace RimTelligence.CustomEvents.Handlers.Pawns.Miracles
{
    internal class MiracleHealChronicCondition : IEffect
    {
        public EffectConfig Config => EffectConfigRegistry.Get("MiracleHealChronicCondition");

        public bool CanBeChoosen(ColonySnapshot snapshot) => snapshot.colonists.Any(colonist =>
                    colonist.health?.chronic_conditions?.Count > 0);

        public GlobalTargetInfo Execute(GameEffect effect, Map map)
        {
            Pawn target = map.mapPawns.FreeColonistsSpawned.FirstOrDefault(pawn =>
                pawn.LabelShort.Equals(effect.TargetPawn, StringComparison.OrdinalIgnoreCase));

            if (target == null)
            {
                ModLogger.Warning($"MiracleHealChronicCondition: no spawned colonist found for '{effect.TargetPawn}'.");
                return GlobalTargetInfo.Invalid;
            }

            var chronicDefNames = target.health.hediffSet.hediffs
                .Where(hediff => hediff.Visible && !(hediff is Hediff_MissingPart) && (hediff.IsPermanent() || hediff.def.chronic))
                .Select(hediff => hediff.def.defName);
            ModLogger.Message($"MiracleHealChronicCondition: {target.LabelShort} chronic conditions: [{string.Join(", ", chronicDefNames)}]");

            HediffDef hediffDef = DefDatabase<HediffDef>.GetNamed(effect.SelectedDefName[0], errorOnFail: false);
            if (hediffDef == null)
            {
                ModLogger.Warning($"MiracleHealChronicCondition: HediffDef '{effect.DefNameText}' not found.");
                return GlobalTargetInfo.Invalid;
            }

            Hediff existing = target.health.hediffSet.hediffs.FirstOrDefault(hediff =>
                hediff.def == hediffDef &&
                !(hediff is Hediff_MissingPart) &&
                (hediff.IsPermanent() || hediff.def.chronic));

            if (existing == null)
            {
                ModLogger.Warning($"MiracleHealChronicCondition: {target.LabelShort} has no chronic condition '{effect.DefNameText}'.");
                return GlobalTargetInfo.Invalid;
            }

            target.health.RemoveHediff(existing);
            ModLogger.Message($"MiracleHealChronicCondition: {target.LabelShort} was cured of {hediffDef.defName}.");
            return new GlobalTargetInfo(target);
        }

        public string GetSummary(GameEffect effect) =>
            $"Chronic condition '{effect.DefNameText}' cured on {effect.TargetPawn}";
    }
}