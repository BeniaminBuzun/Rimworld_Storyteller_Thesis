using RimTelligence.Source.DataSnapshots;
using RimWorld;
using RimWorld.Planet;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;

namespace RimTelligence.Source.CustomEvents.Handlers.Implementations.Pawns
{
    internal class AddExpPointsHandler : IEffectHandler
    {
        public static string XpBonusName;
        public string EffectType => "AddExpPoints";

        public string PromptDescription =>
            "- AddExpPoints : defName = skill name (Intelligence, Shooting etc.), value = 1 - 100, targetPawn = colonist name";
        public string AditionalPromptInfo => $"select only defName from {string.Join(", ", DataList.AllSkillsList)}";

        public GlobalTargetInfo Execute(LlmEffect effect, Map map)
        {
            Pawn target = map.mapPawns.FreeColonistsSpawned.FirstOrDefault(p =>
                p.LabelShort.Equals(effect.targetPawn, StringComparison.OrdinalIgnoreCase));

            if (target == null)
            {
                ModLogger.Warning($"AddExpPoints: no spawned colonist found for '{effect.targetPawn}'.");
                return GlobalTargetInfo.Invalid;
            }

            SkillDef skillDef = DefDatabase<SkillDef>.GetNamed(effect.defName, errorOnFail: false);

            if (skillDef == null)
            {
                ModLogger.Warning($"LoseExpPoints: SkillDef '{effect.defName}' not found.");
                return GlobalTargetInfo.Invalid;
            }

            int expPoints = GetXpReward(target.skills.GetSkill(skillDef).Level, effect.value);


            target.skills.Learn(skillDef, expPoints);

            ModLogger.Message($"AddExpPoints: added {expPoints} XP to '{effect.targetPawn}' for skill '{effect.defName}'.");

            return new GlobalTargetInfo(target);
        }

        private static int GetXpToNextLevel(int level)
        {
            int[] xpTable = new int[]
            {
                1000,2000,3000,4000,5000,
                6000,7000,8000,9000,10000,
                12000,14000,16000,18000,20000,
                22000,24000,26000,28000,30000,
                32000
            };

            return xpTable[Math.Min(level, xpTable.Length - 1)];
        }

        public static int GetXpReward(int level, float value)
        {
            int xpToNext = GetXpToNextLevel(level);

            float factor;
            if (value > 80)
            {
                factor = 0.6f;
                SetXpBonusName("huge");
            }
            else if (value > 51)
            {
                factor = 0.3f;
                SetXpBonusName("medium");
            }

            else
            {
                factor = 0.15f;
                SetXpBonusName("small");
            }
            return (int)(xpToNext * factor);
        }

        public string GetSummary(LlmEffect effect) =>
            $"{effect.targetPawn} got {XpBonusName} bonus exp points for '{effect.defName}'";

        public static void SetXpBonusName(string name)
        {
            XpBonusName = name;
        }
    }
}
