using RimTelligence.DataSnapshots;
using RimWorld.Planet;
using System;
using System.Linq;
using Verse;

namespace RimTelligence.CustomEvents.Handlers.Pawns.Miracles
{
    internal class MiracleHealMissingPart : IEffect
    {
        private readonly EffectConfig _config = EffectConfigRegistry.Get("MiracleHealMissingPart");

        public EffectConfig Config => _config;

        public bool CanBeChoosen(ColonySnapshot snapshot) => snapshot.colonists.Any(colonist =>
                    colonist.health?.missing_body_parts?.Count > 0);

        private string executionSummary;

        public GlobalTargetInfo Execute(GameEffect effect, Map map)
        {
            var candidates = map.mapPawns.FreeColonistsSpawned
                .Where(pawn => pawn.health?.hediffSet?.hediffs?.OfType<Hediff_MissingPart>().Any() == true)
                .ToList();

            Pawn target = null;
            if (!string.IsNullOrWhiteSpace(effect.TargetPawn) && !effect.TargetPawn.Equals("random", StringComparison.OrdinalIgnoreCase))
            {
                target = map.mapPawns.FreeColonistsSpawned.FirstOrDefault(pawn =>
                    pawn.LabelShort.Equals(effect.TargetPawn, StringComparison.OrdinalIgnoreCase));
            }

            if (target == null)
            {
                target = candidates.RandomElementWithFallback() ?? map.mapPawns.FreeColonistsSpawned.RandomElementWithFallback();
            }

            if (target == null)
            {
                ModLogger.Warning($"MiracleHealMissingPart: no spawned colonist found for '{effect.TargetPawn}'.");
                executionSummary = "A wave of regenerative power swept by, but found no injured colonist to mend.";
                return GlobalTargetInfo.Invalid;
            }

            string requestedPart = effect.SelectedDefName != null && effect.SelectedDefName.Length > 0
                ? effect.SelectedDefName[0]
                : effect.DefNameText;

            Hediff_MissingPart missingPart = null;
            if (!string.IsNullOrWhiteSpace(requestedPart))
            {
                missingPart = target.health.hediffSet.hediffs
                    .OfType<Hediff_MissingPart>()
                    .FirstOrDefault(hediff =>
                        hediff.Part != null &&
                        (hediff.Part.LabelCap.ToString().Equals(requestedPart, StringComparison.OrdinalIgnoreCase) ||
                         hediff.Part.def.defName.Equals(requestedPart, StringComparison.OrdinalIgnoreCase)));
            }

            if (missingPart == null)
            {
                missingPart = target.health.hediffSet.hediffs
                    .OfType<Hediff_MissingPart>()
                    .FirstOrDefault(hediff => hediff.Part != null);
            }

            if (missingPart == null)
            {
                ModLogger.Warning($"MiracleHealMissingPart: {target.LabelShort} has no missing part to restore.");
                executionSummary = $"A bright cellular light enveloped {target.LabelShort}, but their body was already whole.";
                return GlobalTargetInfo.Invalid;
            }

            string partLabel = missingPart.Part.LabelCap;
            target.health.RestorePart(missingPart.Part);
            executionSummary = $"Through astonishing tissue regeneration, {target.LabelShort}'s missing {partLabel} has been completely restored!";
            ModLogger.Message($"MiracleHealMissingPart: {target.LabelShort}'s {partLabel} was restored.");

            return new GlobalTargetInfo(target);
        }

        public string GetSummary(GameEffect effect)
        {
            if (!string.IsNullOrEmpty(executionSummary))
                return executionSummary;

            return GetDefaultSummary(effect);
        }

        private static string GetDefaultSummary(GameEffect effect)
        {
            string requestedPart = effect.SelectedDefName != null && effect.SelectedDefName.Length > 0
                ? effect.SelectedDefName[0]
                : (!string.IsNullOrWhiteSpace(effect.DefNameText) ? effect.DefNameText : "body part");
            string pawn = !string.IsNullOrWhiteSpace(effect.TargetPawn) ? effect.TargetPawn : "A colonist";
            return $"Miraculous regeneration: {pawn}'s lost {requestedPart} regrows fully.";
        }
    }
}