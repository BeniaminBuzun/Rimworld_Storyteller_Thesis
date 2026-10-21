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

        private string executionSummary;

        public GlobalTargetInfo Execute(GameEffect effect, Map map)
        {
            var candidates = map.mapPawns.FreeColonistsSpawned
                .Where(pawn => pawn.health?.hediffSet?.hediffs?.Any(h => h.Visible && !(h is Hediff_MissingPart) && (h.IsPermanent() || h.def.chronic)) == true)
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
                ModLogger.Warning($"MiracleHealChronicCondition: no spawned colonist found for '{effect.TargetPawn}'.");
                executionSummary = "A surge of restorative energy rippled through the air, but found no host to heal.";
                return GlobalTargetInfo.Invalid;
            }

            HediffDef hediffDef = ResolveChronicDef(effect);

            Hediff existing = null;
            if (hediffDef != null)
            {
                existing = target.health.hediffSet.hediffs.FirstOrDefault(hediff =>
                    hediff.def == hediffDef &&
                    !(hediff is Hediff_MissingPart) &&
                    (hediff.IsPermanent() || hediff.def.chronic));
            }

            if (existing == null)
            {
                // Fall back to any chronic condition on this target
                existing = target.health.hediffSet.hediffs.FirstOrDefault(hediff =>
                    hediff.Visible &&
                    !(hediff is Hediff_MissingPart) &&
                    (hediff.IsPermanent() || hediff.def.chronic));
            }

            if (existing == null)
            {
                ModLogger.Warning($"MiracleHealChronicCondition: {target.LabelShort} has no chronic condition to cure.");
                executionSummary = $"A healing light washed over {target.LabelShort}, but they had no chronic ailments to cleanse.";
                return GlobalTargetInfo.Invalid;
            }

            string conditionLabel = existing.LabelCap;
            target.health.RemoveHediff(existing);
            executionSummary = $"A medical miracle: {target.LabelShort}'s chronic condition ({conditionLabel}) has been completely healed!";
            ModLogger.Message($"MiracleHealChronicCondition: {target.LabelShort} was cured of {conditionLabel}.");

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
            string conditionLabel = ResolveChronicLabel(effect);
            string pawn = !string.IsNullOrWhiteSpace(effect.TargetPawn) ? effect.TargetPawn : "A colonist";
            return $"A medical miracle: {pawn}'s chronic affliction ({conditionLabel}) is permanently cured.";
        }

        private static HediffDef ResolveChronicDef(GameEffect effect)
        {
            string defName = effect.SelectedDefName != null && effect.SelectedDefName.Length > 0 ? effect.SelectedDefName[0] : effect.DefNameText;
            if (string.IsNullOrWhiteSpace(defName)) return null;
            return DefDatabase<HediffDef>.GetNamed(defName, errorOnFail: false);
        }

        private static string ResolveChronicLabel(GameEffect effect)
        {
            HediffDef def = ResolveChronicDef(effect);
            return def != null ? def.LabelCap.Resolve() : (!string.IsNullOrWhiteSpace(effect.DefNameText) ? effect.DefNameText : "chronic condition");
        }
    }
}