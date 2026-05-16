using RimTelligence.Source.DataSnapshots;
using RimTelligence.Source.CustomEvents.Handlers.Implementations.Pawns;
using RimWorld;
using RimWorld.Planet;
using System;
using System.Linq;
using Verse;

namespace RimTelligence.Source.CustomEvents.Handlers.Implementations.Pawns.Debuffs
{
    internal class LoseExpPointsHandler : IEffectHandler
    {
        public string EffectName => "LoseExpPoints";

        public string NarrativeDescription => "A debuff where a specific colonist loses experience points in a particular skill.";

        public string PromptStructure =>
            "- type = LoseExpPoints:\r\n" +
            "    defName = \"skill name\" (e.g. Shooting, Medicine, Crafting)\r\n" +
            "    count = 0\r\n" +
            "    defDescription = \"\"\r\n" +
            "    value = LOW | MEDIUM | HIGH\r\n" +
            "    targetPawn = \"colonist name\"";

        public string AditionalPromptStructureInfo =>
            $"select only from  {string.Join(", ", GameObjectsCapturer.AllSkillsList.Select(m => $"{m.DefName}: {m.Description}"))}";

        public float BaseWeight => 5f;

        public EventAlignment Alignment => EventAlignment.Bad;

        public GlobalTargetInfo Execute(GameEffect effect, Map map)
        {
            Pawn target = map.mapPawns.FreeColonistsSpawned.FirstOrDefault(p =>
                p.LabelShort.Equals(effect.targetPawn, StringComparison.OrdinalIgnoreCase));

            if (target == null)
            {
                ModLogger.Warning($"LoseExpPoints: no spawned colonist found for '{effect.targetPawn}'.");
                return GlobalTargetInfo.Invalid;
            }

            SkillDef skillDef = DefDatabase<SkillDef>.GetNamed(effect.defName, errorOnFail: false);
            if (skillDef == null)
            {
                ModLogger.Warning($"LoseExpPoints: SkillDef '{effect.defName}' not found.");
                return GlobalTargetInfo.Invalid;
            }

            int expPoints = AddExpPointsHandler.GetXpReward(target.skills.GetSkill(skillDef).Level, effect.value);
            target.skills.Learn(skillDef, -expPoints);

            ModLogger.Message($"LoseExpPoints: removed {expPoints} XP from '{effect.targetPawn}' in skill '{effect.defName}'.");
            return new GlobalTargetInfo(target);
        }

        public string GetSummary(GameEffect effect) =>
            $"{effect.targetPawn} lost a {AddExpPointsHandler.XpBonusName} amount of XP for '{effect.defName}'";
    }
}
