using RimTelligence.Source.CustomEvents.Handlers;
using RimWorld.Planet;
using System;
using System.Linq;
using System.Text;
using Verse;

namespace RimTelligence.CustomEvents.Execution
{
    public static class LlmEffectExecutor
    {
        public static GlobalTargetInfo ExecuteAll(LlmCustomEvent evt, Map map)
        {
            if (evt?.effects == null || !evt.effects.Any())
            {
                ModLogger.Warning("Execute: no effects to run.");
                return GlobalTargetInfo.Invalid;
            }

            ModLogger.Message($"Running {evt.effects.Count} effects...");
            GlobalTargetInfo focusTarget = GlobalTargetInfo.Invalid;

            foreach (var effect in evt.effects)
            {
                var handler = EffectFactory.Create(effect.EffectName);
                if (handler == null)
                {
                    ModLogger.Warning($"Unknown effect type requested by LLM: {effect.EffectName}");
                    continue;
                }

                string defs = effect.SelectedDefName != null && effect.SelectedDefName.Length > 0
                    ? string.Join(", ", effect.SelectedDefName)
                    : "(none)";
                ModLogger.Message($"Executing: {handler.Config.EffectName} (def: {defs})");

                try
                {
                    GlobalTargetInfo result = handler.Execute(effect, map);
                    if (!focusTarget.IsValid && result.IsValid)
                    {
                        focusTarget = result;
                    }
                }
                catch (Exception exception)
                {
                    ModLogger.Error($"Effect {effect.EffectName} failed: {exception.Message}\n{exception.StackTrace}");
                }
            }

            return focusTarget;
        }

        public static string BuildSummary(LlmCustomEvent evt)
        {
            if (evt?.effects == null || !evt.effects.Any()) return string.Empty;

            var lines = new StringBuilder();
            lines.AppendLine("Effects:");

            foreach (var effect in evt.effects)
            {
                var handler = EffectFactory.Create(effect.EffectName);
                string summaryLine = handler != null
                    ? handler.GetSummary(effect)
                    : $"  • Unknown Effect: {effect.EffectName} ({effect.DefNameText})";

                lines.AppendLine(summaryLine);
            }

            return lines.ToString().TrimEnd();
        }
    }
}