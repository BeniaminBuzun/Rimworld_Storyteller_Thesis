using RimTelligence.DataSnapshots;
using RimWorld.Planet;
using System;
using System.Linq;
using Verse;

namespace RimTelligence.CustomEvents.Handlers.Pawns.Miracles
{
    internal class MiracleHealDisease : IEffect
    {
        public EffectConfig Config => EffectConfigRegistry.Get("MiracleHealDisease");

        public bool CanBeChoosen(ColonySnapshot snapshot) => snapshot.colonists.Any(colonist =>
                    colonist.health?.missing_body_parts?.Count > 0);

        public GlobalTargetInfo Execute(GameEffect effect, Map map)
        {
            Pawn target = map.mapPawns.FreeColonistsSpawned.FirstOrDefault(pawn =>
                pawn.LabelShort.Equals(effect.TargetPawn, StringComparison.OrdinalIgnoreCase));

            if (target == null)
            {
                ModLogger.Warning($"MiracleHealDisease: no spawned colonist found for '{effect.TargetPawn}'.");
                return GlobalTargetInfo.Invalid;
            }

            HediffDef hediffDef = DefDatabase<HediffDef>.GetNamed(effect.SelectedDefName[0], errorOnFail: false);
            if (hediffDef == null)
            {
                ModLogger.Warning($"MiracleHealDisease: HediffDef '{effect.DefNameText}' not found.");
                return GlobalTargetInfo.Invalid;
            }

            Hediff existing = target.health.hediffSet.hediffs.FirstOrDefault(hediff =>
                hediff.def == hediffDef &&
                !(hediff is Hediff_MissingPart) &&
                !hediff.IsPermanent() &&
                !hediff.def.chronic);

            if (existing == null)
            {
                ModLogger.Warning($"MiracleHealDisease: {target.LabelShort} has no active disease '{effect.DefNameText}'.");
                return GlobalTargetInfo.Invalid;
            }

            target.health.RemoveHediff(existing);
            ModLogger.Message($"MiracleHealDisease: {target.LabelShort}'s {hediffDef.defName} was healed.");
            return new GlobalTargetInfo(target);
        }

        public string GetSummary(GameEffect effect) =>
            $"Disease '{effect.DefNameText}' healed on {effect.TargetPawn}";
    }
}