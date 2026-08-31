using RimTelligence.DataSnapshots;
using RimWorld;
using RimWorld.Planet;
using System;
using System.Linq;
using Verse;
using static Verse.DamageWorker;

namespace RimTelligence.CustomEvents.Handlers.Pawns.Debuffs
{
    public class DamagePawn : IEffect
    {
        public EffectConfig Config => EffectConfigRegistry.Get("DamagePawn");

        public bool CanBeChoosen(ColonySnapshot snapshot) => true;

        public GlobalTargetInfo Execute(GameEffect effect, Map map)
        {
            DamageDef damageDef = DefDatabase<DamageDef>.GetNamed(effect.SelectedDefName[0], errorOnFail: false)
                ?? DamageDefOf.Burn;

            BodyPartDef partDef = null;
            if (effect.SelectedDefName != null && effect.SelectedDefName.Length >= 2)
                partDef = DefDatabase<BodyPartDef>.GetNamed(effect.SelectedDefName[1], errorOnFail: false);
            else
                ModLogger.Warning($"DamagePawn: effect for '{effect.TargetPawn}' is missing a body part def; falling back to core body part.");

            var allSpawnedColonists = map.mapPawns.FreeColonistsSpawned;
            Pawn target = effect.TargetPawn.Equals("random", StringComparison.OrdinalIgnoreCase)
                ? allSpawnedColonists.RandomElementWithFallback()
                : allSpawnedColonists.FirstOrDefault(pawn =>
                    pawn.LabelShort.Equals(effect.TargetPawn, StringComparison.OrdinalIgnoreCase));

            if (target == null)
            {
                ModLogger.Warning($"DamagePawn: no target found for '{effect.TargetPawn}'.");
                return GlobalTargetInfo.Invalid;
            }

            // Resolve the actual BodyPartRecord on THIS pawn matching the requested def.
            BodyPartRecord hitPart = null;
            if (partDef != null)
            {
                hitPart = target.health.hediffSet.GetNotMissingParts()
                    .Where(p => p.def == partDef)
                    .RandomElementWithFallback();

                if (hitPart == null)
                {
                    ModLogger.Warning($"DamagePawn: {target.LabelShort} has no intact part matching '{effect.SelectedDefName[1]}'. Falling back to core body part.");
                    hitPart = target.RaceProps.body.corePart;
                }
            }
            else
            {
                string missing = effect.SelectedDefName != null && effect.SelectedDefName.Length >= 2
                    ? effect.SelectedDefName[1]
                    : "unknown";
                ModLogger.Warning($"DamagePawn: unknown BodyPartDef '{missing}'. Falling back to core body part.");
                hitPart = target.RaceProps.body.corePart;
            }

            float damageAmount = ResolveDamageForPart(target, hitPart, effect.Severity);

            var dinfo = new DamageInfo(
                def: damageDef,
                amount: damageAmount,
                armorPenetration: -1f,
                angle: -1f,
                instigator: null,
                hitPart: hitPart
            );

            DamageResult result = target.TakeDamage(dinfo);

            ModLogger.Message(
                $"DamagePawn: {damageDef.defName} ({effect.Severity}) on {target.LabelShort}'s {hitPart.Label} — " +
                $"requested={damageAmount:F1}, dealt={result.totalDamageDealt:F1}, " +
                $"deflected={result.deflected}");

            return new GlobalTargetInfo(target);
        }

        private static float ResolveDamageForPart(Pawn pawn, BodyPartRecord part, SeverityTypes severity)
        {
            float partHp = pawn.health.hediffSet.GetPartHealth(part);

            switch (severity)
            {
                case SeverityTypes.Extreme:
                    return partHp * Rand.Range(1.3f, 1.6f);
                case SeverityTypes.High:
                    return partHp * Rand.Range(0.5f, 0.75f);
                case SeverityTypes.Medium:
                    return partHp * Rand.Range(0.25f, 0.5f);
                case SeverityTypes.Low:
                    return partHp * Rand.Range(0.05f, 0.25f);
                default:
                    return 0f;
            }
        }

        public string GetSummary(GameEffect effect) =>
            $"{effect.DefNameText} damage ({effect.Severity}) dealt to {effect.TargetPawn}";
    }
}