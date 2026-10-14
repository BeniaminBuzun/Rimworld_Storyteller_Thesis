using RimTelligence.DataSnapshots;
using RimTelligence.Source.CustomEvents.Handlers.Pawns;
using RimWorld;
using RimWorld.Planet;
using System;
using System.Linq;
using Verse;

namespace RimTelligence.CustomEvents.Handlers.Pawns.Buffs
{
    internal class AddExpPoints : IEffect
    {
        public EffectConfig Config => EffectConfigRegistry.Get("AddExpPoints");

        public bool CanBeChoosen(ColonySnapshot snapshot) => true;

        public GlobalTargetInfo Execute(GameEffect effect, Map map)
        {
            Pawn target = map.mapPawns.FreeColonistsSpawned.FirstOrDefault(pawn =>
                pawn.LabelShort.Equals(effect.TargetPawn, StringComparison.OrdinalIgnoreCase));

            if (target == null)
            {
                ModLogger.Warning($"AddExpPoints: no spawned colonist found for '{effect.TargetPawn}'.");
                return GlobalTargetInfo.Invalid;
            }

            SkillDef skillDef = DefDatabase<SkillDef>.GetNamed(effect.SelectedDefName[0], errorOnFail: false);
            if (skillDef == null)
            {
                ModLogger.Warning($"AddExpPoints: SkillDef '{effect.DefNameText}' not found.");
                return GlobalTargetInfo.Invalid;
            }

            int expPoints = ExpPointsHelper.GetXpReward(target.skills.GetSkill(skillDef).Level, effect.Severity);
            target.skills.Learn(skillDef, expPoints);

            ModLogger.Message($"AddExpPoints: added {effect.Severity} XP to '{effect.TargetPawn}' for skill '{effect.DefNameText}'.");
            return new GlobalTargetInfo(target);
        }

        public string GetSummary(GameEffect effect) =>
            $"{effect.TargetPawn} got a {effect.Severity} XP bonus for '{effect.SelectedDefName[0]}'";
    }
}