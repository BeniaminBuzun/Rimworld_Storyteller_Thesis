using RimWorld;
using RimWorld.Planet;
using Verse;

namespace RimTelligence.Source.CustomEvents.Handlers
{
    public class FireConditionHandler : IEffectHandler
    {
        public string EffectType => "FireCondition";
        public string PromptDescription => "- FireCondition  : defName = GameConditionDef (e.g. HeatWave, ToxicFallout), value = duration days";

        public string AditionalPromptInfo => "select only from these taking into consideration colony state (SolarFlare Eclipse PsychicDrone PsychicSoothe ToxicFallout VolcanicWinter HeatWave ColdSnap Flashstorm ClimateCycle Planetkiller Aurora PsychicDroner)";

        public GlobalTargetInfo Execute(LlmEffect effect, Map map)
        {
            GameConditionDef def = DefDatabase<GameConditionDef>.GetNamed(effect.defName, errorOnFail: false);
            if (def == null)
            {
                ModLogger.Warning($"FireCondition: GameConditionDef '{effect.defName}' not found.");
                return GlobalTargetInfo.Invalid;
            }
            int duration;
            if (effect.value > 0)
            {
                duration = (int)effect.value * 60000;
            }
            else {
                duration = 180000;
            }
            
            GameCondition condition = GameConditionMaker.MakeCondition(def, duration);
            map.gameConditionManager.RegisterCondition(condition);

            ModLogger.Message($"Fired condition: {def.defName} for {duration} days.");

            return GlobalTargetInfo.Invalid;
        }

        public string GetSummary(LlmEffect effect) =>
            $"{effect.defName} condition triggered who knows when will it end";
    }
}