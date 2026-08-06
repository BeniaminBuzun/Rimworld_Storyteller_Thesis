using RimTelligence.DataSnapshots;
using RimWorld;
using RimWorld.Planet;
using System;
using System.Linq;
using Verse;

namespace RimTelligence.CustomEvents.Handlers.Pawns.Buffs
{
    internal class AddExpPointsHandler : IEffectHandler
    {
        public static string XpBonusName;

        public string EffectName => "AddExpPoints";

        public string NarrativeDescription => "An event where a specific colonist receives an experience points bonus.";

        public string PromptStructure =>
            "- type = AddExpPoints:\r\n" +
            "    defName = \"skill name\" (e.g. Shooting, Medicine, Crafting)\r\n" +
            "    count = 0\r\n" +
            "    defDescription = \"\"\r\n" +
            "    value = LOW | MEDIUM | HIGH\r\n" +
            "    targetPawn = \"colonist name\"";

        public string AditionalPromptStructureInfo =>
            $"select only from   {string.Join(", ", GameObjectsCapturer.AllSkillsList.Select(entry => $"{entry.DefName}: {entry.Description}"))}";

        public float BaseWeight => 6f;

        public EventAlignment Alignment => EventAlignment.Good;

        public GlobalTargetInfo Execute(GameEffect effect, Map map)
        {
            Pawn target = map.mapPawns.FreeColonistsSpawned.FirstOrDefault(pawn =>
                pawn.LabelShort.Equals(effect.targetPawn, StringComparison.OrdinalIgnoreCase));

            if (target == null)
            {
                ModLogger.Warning($"AddExpPoints: no spawned colonist found for '{effect.targetPawn}'.");
                return GlobalTargetInfo.Invalid;
            }

            SkillDef skillDef = DefDatabase<SkillDef>.GetNamed(effect.defName, errorOnFail: false);
            if (skillDef == null)
            {
                ModLogger.Warning($"AddExpPoints: SkillDef '{effect.defName}' not found.");
                return GlobalTargetInfo.Invalid;
            }

            int expPoints = GetXpReward(target.skills.GetSkill(skillDef).Level, effect.value);
            target.skills.Learn(skillDef, expPoints);

            ModLogger.Message($"AddExpPoints: added {expPoints} XP to '{effect.targetPawn}' for skill '{effect.defName}'.");
            return new GlobalTargetInfo(target);
        }

        private static int GetXpToNextLevel(int level)
        {
            int[] xpTable =
            {
                1000,  2000,  3000,  4000,  5000,
                6000,  7000,  8000,  9000,  10000,
                12000, 14000, 16000, 18000, 20000,
                22000, 24000, 26000, 28000, 30000,
                32000
            };
            return xpTable[Math.Min(level, xpTable.Length - 1)];
        }

        /// <summary>
        /// Converts a ValueType tier into an XP amount relative to the XP needed
        /// for the pawn's next level.
        ///   LOW    → 15 % of XP-to-next
        ///   MEDIUM → 30 %
        ///   HIGH   → 60 %
        /// </summary>
        public static int GetXpReward(int level, SeverityTypes value)
        {
            int xpToNext = GetXpToNextLevel(level);

            float factor;
            switch (value)
            {
                case SeverityTypes.High:
                    factor = 0.6f;
                    SetXpBonusName("huge");
                    break;
                case SeverityTypes.Medium:
                    factor = 0.3f;
                    SetXpBonusName("medium");
                    break;
                default: // Low or None
                    factor = 0.15f;
                    SetXpBonusName("small");
                    break;
            }

            return (int)(xpToNext * factor);
        }

        public string GetSummary(GameEffect effect) =>
            $"{effect.targetPawn} got a {XpBonusName} XP bonus for '{effect.defName}'";

        public static void SetXpBonusName(string name) => XpBonusName = name;
    }
}