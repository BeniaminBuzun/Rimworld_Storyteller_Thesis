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

        private string executionSummary;

        public GlobalTargetInfo Execute(GameEffect effect, Map map)
        {
            var colonists = map.mapPawns.FreeColonistsSpawned.ToList();
            Pawn target = null;
            if (!string.IsNullOrWhiteSpace(effect.TargetPawn) && !effect.TargetPawn.Equals("random", StringComparison.OrdinalIgnoreCase))
            {
                target = colonists.FirstOrDefault(pawn =>
                    pawn.LabelShort.Equals(effect.TargetPawn, StringComparison.OrdinalIgnoreCase));
            }

            if (target == null)
            {
                target = colonists.RandomElementWithFallback();
            }

            if (target == null)
            {
                ModLogger.Warning($"AddExpPoints: no spawned colonist found for '{effect.TargetPawn}'.");
                executionSummary = "A moment of potential learning passed with no colonist to receive it.";
                return GlobalTargetInfo.Invalid;
            }

            SkillDef skillDef = ResolveSkillDef(effect);
            if (skillDef == null)
            {
                ModLogger.Warning($"AddExpPoints: SkillDef '{effect.DefNameText}' not found.");
                executionSummary = $"{target.LabelShort} felt inspired, but could not channel the experience.";
                return GlobalTargetInfo.Invalid;
            }

            string skillLabel = skillDef.LabelCap.Resolve();
            int expPoints = ExpPointsHelper.GetXpReward(target.skills.GetSkill(skillDef).Level, effect.Severity);
            target.skills.Learn(skillDef, expPoints);

            executionSummary = $"{target.LabelShort} experienced a sudden flash of inspiration, gaining +{expPoints:N0} XP in {skillLabel}!";
            ModLogger.Message($"AddExpPoints: added {expPoints} XP to '{target.LabelShort}' for skill '{skillDef.defName}'.");

            return new GlobalTargetInfo(target);
        }

        public string GetSummary(GameEffect effect)
        {
            if (!string.IsNullOrEmpty(executionSummary))
                return executionSummary;

            return GetDefaultSummary(effect);
        }

        private static string GetDefaultSummary(GameEffect effect)
        {
            string skillLabel = ResolveSkillLabel(effect);
            string pawn = !string.IsNullOrWhiteSpace(effect.TargetPawn) ? effect.TargetPawn : "A colonist";
            string sevStr = effect.Severity != SeverityTypes.None ? $" ({effect.Severity.ToString().ToLower()} boost)" : "";
            return $"{pawn} experiences a surge of insight, gaining XP in {skillLabel}{sevStr}.";
        }

        private static SkillDef ResolveSkillDef(GameEffect effect)
        {
            string defName = effect.SelectedDefName != null && effect.SelectedDefName.Length > 0 ? effect.SelectedDefName[0] : effect.DefNameText;
            if (string.IsNullOrWhiteSpace(defName)) return null;
            return DefDatabase<SkillDef>.GetNamed(defName, errorOnFail: false);
        }

        private static string ResolveSkillLabel(GameEffect effect)
        {
            SkillDef def = ResolveSkillDef(effect);
            return def != null ? def.LabelCap.Resolve() : (!string.IsNullOrWhiteSpace(effect.DefNameText) ? effect.DefNameText : "a skill");
        }
    }
}