using RimTelligence.DataSnapshots;
using RimWorld.Planet;
using System;
using System.Linq;
using Verse;

namespace RimTelligence.CustomEvents.Handlers.Pawns.Miracles
{
    internal class MiracleHealDisease : IEffect
    {
        private readonly EffectConfig _config = EffectConfigRegistry.Get("MiracleHealDisease");

        public EffectConfig Config => _config;

        public bool CanBeChoosen(ColonySnapshot snapshot) => snapshot.colonists.Any(colonist =>
                    colonist.health?.diseases?.Count > 0);

        private string executionSummary;

        public GlobalTargetInfo Execute(GameEffect effect, Map map)
        {
            var candidates = map.mapPawns.FreeColonistsSpawned
                .Where(pawn => pawn.health?.hediffSet?.hediffs?.Any(h => !(h is Hediff_MissingPart) && !h.IsPermanent() && !h.def.chronic && (h.def.isBad || h.Visible)) == true)
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
                ModLogger.Warning($"MiracleHealDisease: no spawned colonist found for '{effect.TargetPawn}'.");
                executionSummary = "A soothing warmth washed through the infirmary, but found no afflicted colonist to heal.";
                return GlobalTargetInfo.Invalid;
            }

            HediffDef hediffDef = ResolveDiseaseDef(effect);

            Hediff existing = null;
            if (hediffDef != null)
            {
                existing = target.health.hediffSet.hediffs.FirstOrDefault(hediff =>
                    hediff.def == hediffDef &&
                    !(hediff is Hediff_MissingPart) &&
                    !hediff.IsPermanent() &&
                    !hediff.def.chronic);
            }

            if (existing == null)
            {
                existing = target.health.hediffSet.hediffs.FirstOrDefault(hediff =>
                    !(hediff is Hediff_MissingPart) &&
                    !hediff.IsPermanent() &&
                    !hediff.def.chronic &&
                    (hediff.def.isBad || hediff.Visible));
            }

            if (existing == null)
            {
                ModLogger.Warning($"MiracleHealDisease: {target.LabelShort} has no active disease to heal.");
                executionSummary = $"A restorative surge energized {target.LabelShort}, but they were already free of infection.";
                return GlobalTargetInfo.Invalid;
            }

            string diseaseLabel = existing.LabelCap;
            target.health.RemoveHediff(existing);
            executionSummary = $"Against all odds, {target.LabelShort}'s infection of {diseaseLabel} has spontaneously cleared and fully healed!";
            ModLogger.Message($"MiracleHealDisease: {target.LabelShort}'s {diseaseLabel} was healed.");

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
            string diseaseLabel = ResolveDiseaseLabel(effect);
            string pawn = !string.IsNullOrWhiteSpace(effect.TargetPawn) ? effect.TargetPawn : "A colonist";
            return $"A miraculous recovery: {pawn}'s infection of {diseaseLabel} has completely healed.";
        }

        private static HediffDef ResolveDiseaseDef(GameEffect effect)
        {
            string defName = effect.SelectedDefName != null && effect.SelectedDefName.Length > 0 ? effect.SelectedDefName[0] : effect.DefNameText;
            if (string.IsNullOrWhiteSpace(defName)) return null;
            return DefDatabase<HediffDef>.GetNamed(defName, errorOnFail: false);
        }

        private static string ResolveDiseaseLabel(GameEffect effect)
        {
            HediffDef def = ResolveDiseaseDef(effect);
            return def != null ? def.LabelCap.Resolve() : (!string.IsNullOrWhiteSpace(effect.DefNameText) ? effect.DefNameText : "a disease");
        }
    }
}