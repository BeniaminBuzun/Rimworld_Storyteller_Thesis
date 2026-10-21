using RimTelligence.DataSnapshots;
using RimWorld;
using RimWorld.Planet;
using System;
using Verse;
namespace RimTelligence.CustomEvents.Handlers.Environment
{
    public class StartExtremeWeather : IEffect
    {
        private static readonly Random _random = new Random();
        private const int TICKS_PER_DAY = 60000;
        public EffectConfig Config => EffectConfigRegistry.Get("StartExtremeWeather");

        private string executionSummary;

        public bool CanBeChoosen(ColonySnapshot snapshot) => true;

        public GlobalTargetInfo Execute(GameEffect effect, Map map)
        {
            GameConditionDef def = ResolveCondition(effect);
            if (def == null)
            {
                ModLogger.Warning($"StartExtremeWeather: GameConditionDef '{effect.DefNameText}' not found.");
                executionSummary = "An ominous atmospheric anomaly loomed over the horizon, but dissipated without incident.";
                return GlobalTargetInfo.Invalid;
            }

            int numDays = Math.Max(1, CalculateDuration(effect.Severity));
            int duration = numDays * TICKS_PER_DAY;

            GameCondition condition = GameConditionMaker.MakeCondition(def, duration);
            map.gameConditionManager.RegisterCondition(condition);

            string conditionLabel = def.LabelCap.Resolve();
            executionSummary = $"{conditionLabel} has begun to ravage the area (expected duration: ~{numDays} day{(numDays == 1 ? "" : "s")}).";

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
                default: return _random.Next(1, 3);
            }
        }

        public string GetSummary(GameEffect effect)
        {
            if (!string.IsNullOrEmpty(executionSummary))
                return executionSummary;

            return GetDefaultSummary(effect);
        }

        private static string GetDefaultSummary(GameEffect effect)
        {
            GameConditionDef def = ResolveCondition(effect);
            string conditionLabel = def != null ? def.LabelCap.Resolve() : (!string.IsNullOrWhiteSpace(effect.DefNameText) ? effect.DefNameText : "an extreme condition");
            string sevStr = effect.Severity != SeverityTypes.None ? $" ({effect.Severity.ToString().ToLower()})" : "";
            return $"{conditionLabel} descends upon the colony{sevStr}.";
        }

        private static GameConditionDef ResolveCondition(GameEffect effect)
        {
            string defName = effect.SelectedDefName != null && effect.SelectedDefName.Length > 0 ? effect.SelectedDefName[0] : effect.DefNameText;
            if (string.IsNullOrWhiteSpace(defName)) return null;
            return DefDatabase<GameConditionDef>.GetNamed(defName, errorOnFail: false);
        }
    }
}