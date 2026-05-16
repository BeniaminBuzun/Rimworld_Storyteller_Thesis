using System;
using System.Linq;
using System.Text;
using RimWorld.Planet;
using Verse;
using RimTelligence.Source.CustomEvents.Handlers;

namespace RimTelligence.Source.CustomEvents.Execution
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
                var handler = EffectHandlerRegistry.GetHandler(effect.type);
                if (handler == null)
                {
                    ModLogger.Warning($"Unknown effect type requested by LLM: {effect.type}");
                    continue;
                }

                ModLogger.Message($"Executing: {handler.EffectName} (def: {effect.defName}, count: {effect.count})");

                try
                {
                    GlobalTargetInfo result = handler.Execute(effect, map);
                    if (!focusTarget.IsValid && result.IsValid)
                    {
                        focusTarget = result;
                    }
                }
                catch (Exception e)
                {
                    ModLogger.Error($"Effect {effect.type} failed: {e.Message}\n{e.StackTrace}");
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
                var handler = EffectHandlerRegistry.GetHandler(effect.type);
                string summaryLine = handler != null
                    ? handler.GetSummary(effect)
                    : $"  • Unknown Effect: {effect.type} ({effect.defName})";

                lines.AppendLine(summaryLine);
            }

            return lines.ToString().TrimEnd();
        }
    }
}