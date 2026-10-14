using RimTelligence.Source.CustomEvents.Handlers;
using RimWorld.Planet;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Verse;

namespace RimTelligence.CustomEvents.Execution
{
    public readonly struct EffectOutcome
    {
        public readonly string Label;
        public readonly GlobalTargetInfo Target;

        public EffectOutcome(string label, GlobalTargetInfo target)
        {
            Label = label;
            Target = target;
        }
    }
    public static class LlmEffectExecutor
    {
        public static List<EffectOutcome> ExecuteAll(LlmCustomEvent evt, Map map)
        {
            var outcomes = new List<EffectOutcome>();

            if (evt?.effects == null || !evt.effects.Any())
            {
                ModLogger.Warning("Execute: no effects to run.");
                return outcomes;
            }

            ModLogger.Message($"Running {evt.effects.Count} effects...");

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
                    if (result.IsValid)
                    {
                        outcomes.Add(new EffectOutcome(handler.Config.EffectName, result));
                    }
                }
                catch (Exception exception)
                {
                    ModLogger.Error($"Effect {effect.EffectName} failed: {exception.Message}\n{exception.StackTrace}");
                }
            }

            return outcomes;
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