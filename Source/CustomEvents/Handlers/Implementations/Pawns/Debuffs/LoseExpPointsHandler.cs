using RimTelligence.Source.DataSnapshots;
using RimWorld;
using RimWorld.Planet;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;

namespace RimTelligence.Source.CustomEvents.Handlers.Implementations.Pawns.Debuffs
{
    internal class LoseExpPointsHandler : IEffectHandler
    {
        public string EffectType => "LoseExpPoints";

        public string PromptDescription =>
            "- LoseExpPoints : defName = skill name (Intelligence, Shooting etc.), value = 1 - 100, targetPawn = colonist name";
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


            int expPoints = AddExpPointsHandler.GetXpReward(target.skills.GetSkill(skillDef).Level, effect.value); // I don't like this structire but what the hell


            target.skills.Learn(skillDef, -expPoints);

            ModLogger.Message($"LoseExpPoints: lost {expPoints} XP for '{effect.targetPawn}' in skill '{effect.defName}'.");

            return new GlobalTargetInfo(target);
        }

        public string GetSummary(LlmEffect effect) =>
            $"{effect.targetPawn} lost {AddExpPointsHandler.XpBonusName} amount of exp points for '{effect.defName}'"; // turbo lazy rewrite it fully in the future xd 
    }
}
