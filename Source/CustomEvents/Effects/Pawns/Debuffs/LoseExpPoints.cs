using RimTelligence.DataSnapshots;
using RimTelligence.Source.CustomEvents.Handlers.Pawns;
using RimWorld;
using RimWorld.Planet;
using System;
using System.Linq;
using Verse;


namespace RimTelligence.CustomEvents.Handlers.Pawns.Debuffs
{
    internal class LoseExpPoints : IEffect
    {
        private readonly EffectConfig _config = EffectConfigRegistry.Get("LoseExpPoints");

        public EffectConfig Config => _config;

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
                ModLogger.Warning($"LoseExpPoints: no spawned colonist found for '{effect.TargetPawn}'.");
                executionSummary = "A wave of confusion swept through the settlement, but left all colonists unaffected.";
                return GlobalTargetInfo.Invalid;
            }

            SkillDef skillDef = ResolveSkillDef(effect);
            if (skillDef == null)
            {
                ModLogger.Warning($"LoseExpPoints: SkillDef '{effect.DefNameText}' not found.");
                executionSummary = $"{target.LabelShort} felt disoriented, but shook off the mental haze without losing skills.";
                return GlobalTargetInfo.Invalid;
            }

            string skillLabel = skillDef.LabelCap.Resolve();
            int expPoints = ExpPointsHelper.GetXpReward(target.skills.GetSkill(skillDef).Level, effect.Severity);
            target.skills.Learn(skillDef, -expPoints);

            executionSummary = $"{target.LabelShort} suffered mental fatigue and confusion, losing {expPoints:N0} XP in {skillLabel}!";
            ModLogger.Message($"LoseExpPoints: removed {expPoints} XP from '{target.LabelShort}' in skill '{skillDef.defName}'.");

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
            string sevStr = $" ({effect.Severity.ToString().ToLower()})";
            return $"{pawn} experiences disorientation, losing XP in {skillLabel}{sevStr}.";
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