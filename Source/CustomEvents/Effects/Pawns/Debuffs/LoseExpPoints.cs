using RimTelligence.DataSnapshots;
using RimTelligence.CustomEvents.Handlers.Pawns.Buffs;
using RimWorld;
using RimWorld.Planet;
using System;
using System.Linq;
using Verse;
using RimTelligence.Source.CustomEvents.Handlers.Pawns;


namespace RimTelligence.CustomEvents.Handlers.Pawns.Debuffs
{
    internal class LoseExpPoints : IEffect
    {
        public EffectConfig Config => EffectConfigRegistry.Get("LoseExpPoints");

        public bool CanBeChoosen(ColonySnapshot snapshot) => true;

        public GlobalTargetInfo Execute(GameEffect effect, Map map)
        {
            Pawn target = map.mapPawns.FreeColonistsSpawned.FirstOrDefault(pawn =>
                pawn.LabelShort.Equals(effect.TargetPawn, StringComparison.OrdinalIgnoreCase));

            if (target == null)
            {
                ModLogger.Warning($"LoseExpPoints: no spawned colonist found for '{effect.TargetPawn}'.");
                return GlobalTargetInfo.Invalid;
            }

            SkillDef skillDef = DefDatabase<SkillDef>.GetNamed(effect.SelectedDefName[0], errorOnFail: false);
            if (skillDef == null)
            {
                ModLogger.Warning($"LoseExpPoints: SkillDef '{effect.DefNameText}' not found.");
                return GlobalTargetInfo.Invalid;
            }

            int expPoints = ExpPointsHelper.GetXpReward(target.skills.GetSkill(skillDef).Level, effect.Severity);
            target.skills.Learn(skillDef, -expPoints);

            ModLogger.Message($"LoseExpPoints: removed {expPoints} XP from '{effect.TargetPawn}' in skill '{effect.DefNameText}'.");
            return new GlobalTargetInfo(target);
        }

        public string GetSummary(GameEffect effect) =>
            $"{effect.TargetPawn} lost a {AddExpPoints.XpBonusName} amount of XP for '{effect.DefNameText}'";
    }
}