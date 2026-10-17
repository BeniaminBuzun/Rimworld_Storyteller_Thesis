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

        private string executionSummary;

        public GlobalTargetInfo Execute(GameEffect effect, Map map)
        {
            string damageName = effect.SelectedDefName != null && effect.SelectedDefName.Length > 0
                ? effect.SelectedDefName[0]
                : (!string.IsNullOrWhiteSpace(effect.DefNameText) ? effect.DefNameText : null);

            DamageDef damageDef = (!string.IsNullOrWhiteSpace(damageName)
                ? DefDatabase<DamageDef>.GetNamed(damageName, errorOnFail: false)
                : null) ?? DamageDefOf.Burn;

            BodyPartDef partDef = null;
            if (effect.SelectedDefName != null && effect.SelectedDefName.Length >= 2 && !string.IsNullOrWhiteSpace(effect.SelectedDefName[1]))
                partDef = DefDatabase<BodyPartDef>.GetNamed(effect.SelectedDefName[1], errorOnFail: false);
            else
                ModLogger.Warning($"DamagePawn: effect for '{effect.TargetPawn}' is missing a body part def; falling back to core body part.");

            var allSpawnedColonists = map.mapPawns.FreeColonistsSpawned.ToList();
            Pawn target = null;
            if (!string.IsNullOrWhiteSpace(effect.TargetPawn) && !effect.TargetPawn.Equals("random", StringComparison.OrdinalIgnoreCase))
            {
                target = allSpawnedColonists.FirstOrDefault(pawn =>
                    pawn.LabelShort.Equals(effect.TargetPawn, StringComparison.OrdinalIgnoreCase));
            }

            if (target == null)
            {
                target = allSpawnedColonists.RandomElementWithFallback();
            }

            if (target == null)
            {
                ModLogger.Warning($"DamagePawn: no target found for '{effect.TargetPawn}'.");
                executionSummary = "A sudden violent impact shook the ground, but missed all colonists.";
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

            string partLabel = hitPart != null ? hitPart.Label : "body";
            string dmgLabel = damageDef.LabelCap.Resolve();

            if (result.deflected)
            {
                executionSummary = $"{target.LabelShort}'s armor deflected a dangerous {dmgLabel} blow to their {partLabel}!";
            }
            else
            {
                executionSummary = $"{target.LabelShort} sustained {result.totalDamageDealt:F0} {dmgLabel} damage to their {partLabel} ({effect.Severity.ToString().ToLower()})!";
            }

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
                    return partHp * Rand.Range(0.15f, 0.30f);
            }
        }

        public string GetSummary(GameEffect effect)
        {
            if (!string.IsNullOrEmpty(executionSummary))
                return executionSummary;

            return GetDefaultSummary(effect);
        }

        private static string GetDefaultSummary(GameEffect effect)
        {
            string damageName = effect.SelectedDefName != null && effect.SelectedDefName.Length > 0
                ? effect.SelectedDefName[0]
                : (!string.IsNullOrWhiteSpace(effect.DefNameText) ? effect.DefNameText : "physical");
            DamageDef damageDef = DefDatabase<DamageDef>.GetNamed(damageName, errorOnFail: false);
            string dmgLabel = damageDef != null ? damageDef.LabelCap.Resolve() : damageName;

            string partName = effect.SelectedDefName != null && effect.SelectedDefName.Length >= 2
                ? effect.SelectedDefName[1]
                : "body";
            BodyPartDef partDef = DefDatabase<BodyPartDef>.GetNamed(partName, errorOnFail: false);
            string partLabel = partDef != null ? partDef.LabelCap.Resolve() : partName;

            string pawn = !string.IsNullOrWhiteSpace(effect.TargetPawn) ? effect.TargetPawn : "A colonist";
            string sevStr = effect.Severity != SeverityTypes.None ? $" ({effect.Severity.ToString().ToLower()})" : "";
            return $"{pawn} suffers {dmgLabel} trauma to their {partLabel}{sevStr}.";
        }
    }
}