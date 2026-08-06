using RimTelligence.DataSnapshots;
using RimWorld;
using RimWorld.Planet;
using System;
using System.Linq;
using Verse;
namespace RimTelligence.CustomEvents.Handlers.Pawns.Debuffs
{
    public class DamagePawnHandler : IEffectHandler
    {
        public string EffectName => "DamagePawn";

        public string NarrativeDescription => "An event where a specific colonist takes damage.";

        public string PromptStructure =>
            "- type = DamagePawn:\r\n" +
            "    defName = \"DamageDef name\" (e.g. Burn, Bullet, Blunt)\r\n" +
            "    count = 0\r\n" +
            "    defDescription = \"\"\r\n" +
            "    value = LOW | MEDIUM | HIGH\r\n" +
            "    targetPawn = \"colonist name or 'random'\"";

        public string AditionalPromptStructureInfo =>
            $"select only from {string.Join(", ", GameObjectsCapturer.DamageTypeList.Select(entry => $"{entry.DefName}: {entry.Description}"))}";

        public float BaseWeight => 8f;

        public EventAlignment Alignment => EventAlignment.Bad;

        public GlobalTargetInfo Execute(GameEffect effect, Map map)
        {
            DamageDef damageDef = DefDatabase<DamageDef>.GetNamed(effect.defName, errorOnFail: false) ?? DamageDefOf.Burn;

            var allSpawnedColonists = map.mapPawns.FreeColonistsSpawned;
            Pawn target = effect.targetPawn.Equals("random", StringComparison.OrdinalIgnoreCase)
                ? allSpawnedColonists.RandomElementWithFallback()
                : allSpawnedColonists.FirstOrDefault(pawn =>
                    pawn.LabelShort.Equals(effect.targetPawn, StringComparison.OrdinalIgnoreCase));

            if (target == null)
            {
                ModLogger.Warning($"DamagePawn: no target found for '{effect.targetPawn}'.");
                return GlobalTargetInfo.Invalid;
            }

            float damageAmount = ResolveDamage(effect.value);
            target.TakeDamage(new DamageInfo(damageDef, damageAmount));

            ModLogger.Message($"DamagePawn: dealt {damageAmount} {damageDef.defName} to {target.LabelShort} (value={effect.value}).");
            return new GlobalTargetInfo(target);
        }

        private static float ResolveDamage(SeverityTypes value)
        {
            switch (value)
            {
                case SeverityTypes.High:   return 30f;
                case SeverityTypes.Medium: return 15f;
                default:               return 5f;  // Low or None
            }
        }

        public string GetSummary(GameEffect effect) =>
            $"{effect.defName} damage ({effect.value}) dealt to {effect.targetPawn}";
    }
}