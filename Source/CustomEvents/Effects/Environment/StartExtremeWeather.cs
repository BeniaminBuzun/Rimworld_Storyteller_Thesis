using RimTelligence.DataSnapshots;
using RimWorld;
using RimWorld.Planet;
using System;
using Verse;
namespace RimTelligence.CustomEvents.Handlers.Environment
{
    public class StartExtremeWeather : IEffect
    {
        public EffectConfig Config => EffectConfigRegistry.Get("StartExtremeWeatherHandler");
        private static readonly Random _random = new Random();
        private int TICKS_PER_DAY = 60000;

        public bool CanBeChoosen(ColonySnapshot snapshot) => true;

        public GlobalTargetInfo Execute(GameEffect effect, Map map)
        {
            GameConditionDef def = DefDatabase<GameConditionDef>.GetNamed(effect.SelectedDefName[0], errorOnFail: false);
            if (def == null)
            {
                ModLogger.Warning($"StartExtremeWeatherEvent: GameConditionDef '{effect.DefNameText}' not found.");
                return GlobalTargetInfo.Invalid;
            }

            int numDays = CalculateDuration(effect.Severity);
            int duration = numDays * TICKS_PER_DAY;

            GameCondition condition = GameConditionMaker.MakeCondition(def, duration);
            map.gameConditionManager.RegisterCondition(condition);

            ModLogger.Message($"Fired condition: {def.defName} for {numDays} day(s) ({duration} ticks).");

            return GlobalTargetInfo.Invalid;
        }

        private int CalculateDuration(SeverityTypes severity)
        {
            switch (severity)
            {
                case SeverityTypes.Low: return 1;
                case SeverityTypes.Medium: return _random.Next(2, 4);
                case SeverityTypes.High: return _random.Next(4, 6);
                case SeverityTypes.Extreme: return _random.Next(7, 16);
                default: return 0;
            }
        }

        public string GetSummary(GameEffect effect) =>
            $"{effect.DefNameText} condition triggered for only all mighty machine know how long";
    }
}