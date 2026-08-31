using RimTelligence.CustomEvents;
using RimTelligence.CustomEvents.Handlers;
using RimTelligence.DataSnapshots;
using RimWorld.Planet;
using System;
using System.Linq;
using Verse;

namespace RimTelligence.CustomEvents.Handlers.Pawns.Miracles
{
    internal class MiracleHealMissingPart : IEffect
    {
        public EffectConfig Config => EffectConfigRegistry.Get("MiracleHealMissingPart");

        public bool CanBeChoosen(ColonySnapshot snapshot) => snapshot.colonists.Any(colonist =>
                    colonist.health?.missing_body_parts?.Count > 0);

        public GlobalTargetInfo Execute(GameEffect effect, Map map)
        {
            Pawn target = map.mapPawns.FreeColonistsSpawned.FirstOrDefault(pawn =>
                pawn.LabelShort.Equals(effect.TargetPawn, StringComparison.OrdinalIgnoreCase));

            if (target == null)
            {
                ModLogger.Warning($"MiracleHealMissingPart: no spawned colonist found for '{effect.TargetPawn}'.");
                return GlobalTargetInfo.Invalid;
            }

            Hediff_MissingPart missingPart = target.health.hediffSet.hediffs
                .OfType<Hediff_MissingPart>()
                .FirstOrDefault(hediff =>
                    hediff.Part != null &&
                    hediff.Part.LabelCap.ToString().Equals(effect.SelectedDefName[0], StringComparison.OrdinalIgnoreCase));

            if (missingPart == null)
            {
                ModLogger.Warning($"MiracleHealMissingPart: {target.LabelShort} has no missing part '{effect.DefNameText}'.");
                return GlobalTargetInfo.Invalid;
            }

            target.health.RestorePart(missingPart.Part);
            ModLogger.Message($"MiracleHealMissingPart: {target.LabelShort}'s {missingPart.Part.LabelCap} was restored.");
            return new GlobalTargetInfo(target);
        }

        public string GetSummary(GameEffect effect) =>
            $"Missing part '{effect.DefNameText}' restored on {effect.TargetPawn}";
    }
}