using RimTelligence.DataSnapshots;
using RimWorld.Planet;
using Verse;

namespace RimTelligence.CustomEvents.Handlers.Environment
{
    internal class ChangeWeather : IEffect
    {
        public EffectConfig Config => EffectConfigRegistry.Get("ChangeWeather");

        public bool CanBeChoosen(ColonySnapshot snapshot) => true;

        public GlobalTargetInfo Execute(GameEffect effect, Map map)
        {
            WeatherDef def = DefDatabase<WeatherDef>.GetNamed(effect.SelectedDefName[0], errorOnFail: false);
            if (def == null)
            {
                ModLogger.Warning($"ChangeWeather: WeatherDef '{effect.DefNameText}' not found.");
                return GlobalTargetInfo.Invalid;
            }

            map.weatherManager.curWeather = def;

            ModLogger.Message($"Sudden weather change: {def.defName}.");

            return GlobalTargetInfo.Invalid;
        }

        public string GetSummary(GameEffect effect)
        {
            return $"Sudden weather change: {effect.DefNameText}.";
        }
    }
}