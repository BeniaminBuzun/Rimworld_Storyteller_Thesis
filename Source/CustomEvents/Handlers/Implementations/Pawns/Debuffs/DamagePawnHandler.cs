using RimTelligence.Source.DataSnapshots;
using RimWorld;
using RimWorld.Planet;
using System;
using System.Linq;
using Verse;


namespace RimTelligence.Source.CustomEvents.Handlers
{
    public class DamagePawnHandler : IEffectHandler
    {
        public string EffectType => "DamagePawn";

        public string PromptDescription => "- DamagePawn    : defName = DamageDef (e.g. Burn, Bullet), value = damage, targetPawn = name or \"random\"";

        public string AditionalPromptInfo => $"select only from these damage types {string.Join(", ", DataList.DamageTypeList)}";

        public GlobalTargetInfo Execute(LlmEffect effect, Map map)
        {
            DamageDef damageDef = DefDatabase<DamageDef>.GetNamed(effect.defName, errorOnFail: false) ?? DamageDefOf.Burn;

            var allSpawnedColonists = map.mapPawns.FreeColonistsSpawned;
            Pawn target = null;

            if (effect.targetPawn.Equals("random", StringComparison.OrdinalIgnoreCase))
            {
                target = allSpawnedColonists.RandomElementWithFallback();
            }
            else
            {
                target = allSpawnedColonists.FirstOrDefault(p => p.LabelShort.Equals(effect.targetPawn, StringComparison.OrdinalIgnoreCase));
            }

            if (target == null)
            {
                ModLogger.Warning($"DamagePawns: No target found for '{effect.targetPawn}'.");
                return GlobalTargetInfo.Invalid;
            }

            target.TakeDamage(new DamageInfo(damageDef, effect.value));
            ModLogger.Message($"Damaged {target.LabelShort} for {effect.value} {damageDef.defName}");

            return new GlobalTargetInfo(target);
        }

        public string GetSummary(LlmEffect effect) =>
            $"{effect.value} {effect.defName} damage dealt to {effect.targetPawn}";
    }
}