using RimTelligence.Source.DataSnapshots;
using RimWorld;
using RimWorld.Planet;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;

namespace RimTelligence.Source.CustomEvents.Handlers.Implementations.Environment
{
    internal class ChangeWeatherHandler : IEffectHandler
    {
        public string EffectType => "ChangeWeather";

        public string PromptDescription => "- ChangeWeather        : defName = WeatherDef ()";

        public string AditionalPromptInfo => $"select only from these weather {string.Join(", ", DataList.AllWeathersList)}";

        public GlobalTargetInfo Execute(LlmEffect effect, Map map)
        {
            WeatherDef def = DefDatabase<WeatherDef>.GetNamed(effect.defName, errorOnFail: false);
            if (def == null)
            {
                ModLogger.Warning($"ChangeWeather: WeatherDef '{effect.defName}' not found.");
                return GlobalTargetInfo.Invalid;
            }

            map.weatherManager.curWeather = def;

            ModLogger.Message($"Sudden weather change: {def.defName}.");

            return GlobalTargetInfo.Invalid;
        }

        public string GetSummary(LlmEffect effect)
        {
            return $"Sudden weather change: {effect.defName}.";
        }
    }
}
