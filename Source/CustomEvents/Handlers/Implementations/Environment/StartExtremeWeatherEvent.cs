using RimTelligence.Source.DataSnapshots;
using RimWorld;
using RimWorld.Planet;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;
namespace RimTelligence.Source.CustomEvents.Handlers
{
    public class StartExtremeWeatherEvent : IEffectHandler
    {
        public string EffectType => "StartExtremeWeatherEvent";
        public string PromptDescription => "- StartExtremeWeatherEvent  : defName = GameConditionDef (e.g. HeatWave, ToxicFallout), value = duration days";

        public string AditionalPromptInfo => $"select only from these conditions {string.Join(", ", DataList.GameConditionList)}";

        public GlobalTargetInfo Execute(LlmEffect effect, Map map)
        {
            GameConditionDef def = DefDatabase<GameConditionDef>.GetNamed(effect.defName, errorOnFail: false);
            if (def == null)
            {
                ModLogger.Warning($"StartExtremeWeatherEvent: GameConditionDef '{effect.defName}' not found.");
                return GlobalTargetInfo.Invalid;
            }
            int duration;
            if (effect.value > 0)
            {
                duration = (int)(effect.value * 60000); // num of days to ticks
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