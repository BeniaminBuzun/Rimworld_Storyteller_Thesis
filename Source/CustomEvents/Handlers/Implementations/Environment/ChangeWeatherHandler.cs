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
        public string EffectName => "ChangeWeather";

        public string NarrativeDescription => $"An event where a weather quickly changes.";

        public string PromptStructure => "- type = ChangeWeather:\r\n    defName = \"WeatherDef name\" (string)\r\n    count = 0\r\n    defDescription = \"\"\r\n    value = NONE\r\n    targetPawn = \"\"";

        public string AditionalPromptStructureInfo =>
            $"select only from {string.Join(", ", GameObjectsCapturer.AllWeathersList.Select(m => $"{m.DefName}: {m.Description}"))}";

        public float BaseWeight => 8f;

        public EventAlignment Alignment => EventAlignment.Neutral;

        public GlobalTargetInfo Execute(GameEffect effect, Map map)
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

        public string GetSummary(GameEffect effect)
        {
            return $"Sudden weather change: {effect.defName}.";
        }
    }
}
