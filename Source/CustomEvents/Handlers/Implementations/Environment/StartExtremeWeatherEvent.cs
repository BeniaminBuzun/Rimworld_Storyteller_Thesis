using RimTelligence.Source.DataSnapshots;
using RimWorld;
using RimWorld.Planet;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using UnityEngine;
using Verse;
namespace RimTelligence.Source.CustomEvents.Handlers
{
    public class StartExtremeWeatherEvent : IEffectHandler
    {
        public string EffectName => "StartExtremeWeatherEvent";

        public string NarrativeDescription => $"An event where an extreame weather condition starts.";


        public string PromptStructure => "- type = StartExtremeWeatherEvent:\r\n    defName =GameConditionDef \r\n    count = duration days (int)\r\n    defDescription = \"\"\r\n    value = NONE\r\n    targetPawn = \"\"";

        public string AditionalPromptStructureInfo =>
            $"select only from {string.Join(", ", GameObjectsCapturer.GameConditionList.Select(m => $"{m.DefName}: {m.Description}"))}";

        public float BaseWeight => 3f;

        public EventAlignment Alignment => EventAlignment.Bad;

        public GlobalTargetInfo Execute(GameEffect effect, Map map)
        {
            GameConditionDef def = DefDatabase<GameConditionDef>.GetNamed(effect.defName, errorOnFail: false);
            if (def == null)
            {
                ModLogger.Warning($"StartExtremeWeatherEvent: GameConditionDef '{effect.defName}' not found.");
                return GlobalTargetInfo.Invalid;
            }
            int duration;

            duration = (int)(effect.count * 60000); // num of days to ticks
                
            GameCondition condition = GameConditionMaker.MakeCondition(def, duration);
            map.gameConditionManager.RegisterCondition(condition);

            ModLogger.Message($"Fired condition: {def.defName} for {duration} days.");

            return GlobalTargetInfo.Invalid;
        }

        public string GetSummary(GameEffect effect) =>
            $"{effect.defName} condition triggered for {effect.count} days.";
    }
}