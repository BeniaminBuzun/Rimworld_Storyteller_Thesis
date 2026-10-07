using RimTelligence.DataSnapshots;
using RimWorld.Planet;
using System;
using System.Linq;
using Verse;

namespace RimTelligence.CustomEvents.Handlers.Pawns.Debuffs
{
    public class AddDisease : IEffect
    {
        public EffectConfig Config => EffectConfigRegistry.Get("AddDisease");
        private string specialSummary;

        public bool CanBeChoosen(ColonySnapshot snapshot) => true;

        public GlobalTargetInfo Execute(GameEffect effect, Map map)
        {
            HediffDef hediffDef = DefDatabase<HediffDef>.GetNamed(effect.SelectedDefName[0], errorOnFail: false);
            if (hediffDef == null)
            {
                ModLogger.Warning($"DiseasePawn: HediffDef '{effect.DefNameText}' not found.");
                return GlobalTargetInfo.Invalid;
            }

            Pawn target = map.mapPawns.FreeColonistsSpawned
                .FirstOrDefault(pawn => pawn.LabelShort.Equals(effect.TargetPawn, StringComparison.OrdinalIgnoreCase));

            if (target == null)
            {
                ModLogger.Warning($"DiseasePawn: no spawned colonist found for '{effect.TargetPawn}'.");
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