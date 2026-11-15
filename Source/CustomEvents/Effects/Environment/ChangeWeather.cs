using RimTelligence.DataSnapshots;
using RimWorld.Planet;
using Verse;

namespace RimTelligence.CustomEvents.Handlers.Environment
{
    internal class ChangeWeather : IEffect
    {
        private readonly EffectConfig _config = EffectConfigRegistry.Get("ChangeWeather");

        public EffectConfig Config => _config;

        public bool CanBeChoosen(ColonySnapshot snapshot) => true;

        private string executionSummary;

        public GlobalTargetInfo Execute(GameEffect effect, Map map)
        {
            WeatherDef def = ResolveWeather(effect);
            if (def == null)
            {
                ModLogger.Warning($"ChangeWeather: WeatherDef '{effect.DefNameText}' not found.");
                executionSummary = "The winds howled momentarily, but the weather remained unchanged.";
                return GlobalTargetInfo.Invalid;
            }

            map.weatherManager.TransitionTo(def);
            string weatherLabel = def.LabelCap.Resolve();
            executionSummary = $"Atmospheric pressure shifted abruptly: the weather turned to {weatherLabel}.";

            ModLogger.Message($"Sudden weather change: {def.defName}.");

            return GlobalTargetInfo.Invalid;
        }

        public string GetSummary(GameEffect effect)
        {
            if (!string.IsNullOrEmpty(executionSummary))
                return executionSummary;

            return GetDefaultSummary(effect);
        }

        private static string GetDefaultSummary(GameEffect effect)
        {
            WeatherDef def = ResolveWeather(effect);
            string weatherLabel = def != null ? def.LabelCap.Resolve() : (!string.IsNullOrWhiteSpace(effect.DefNameText) ? effect.DefNameText : "unusual weather");
            return $"The atmosphere shifts dramatically as skies turn into {weatherLabel}.";
        }

        private static WeatherDef ResolveWeather(GameEffect effect)
        {
            string defName = effect.SelectedDefName != null && effect.SelectedDefName.Length > 0 ? effect.SelectedDefName[0] : effect.DefNameText;
            if (string.IsNullOrWhiteSpace(defName)) return null;
            return DefDatabase<WeatherDef>.GetNamed(defName, errorOnFail: false);
        }
    }
}