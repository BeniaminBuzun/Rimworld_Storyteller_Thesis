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
        public readonly string Summary;

        public EffectOutcome(string label, GlobalTargetInfo target, string summary = null)
        {
            Label = label;
            Target = target;
            Summary = summary;
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
                    string summary = handler.GetSummary(effect);
                    outcomes.Add(new EffectOutcome(handler.Config.EffectName, result, summary));
                }
                catch (Exception exception)
                {
                    ModLogger.Error($"Effect {effect.EffectName} failed: {exception.Message}\n{exception.StackTrace}");
                }
            }

            return outcomes;
        }

        public static string BuildSummary(LlmCustomEvent evt, List<EffectOutcome> outcomes = null)
        {
            if (evt?.effects == null || !evt.effects.Any()) return string.Empty;

            var lines = new StringBuilder();
            lines.AppendLine("Effects:");

            if (outcomes != null && outcomes.Count > 0)
            {
                foreach (var outcome in outcomes)
                {
                    string summaryLine = outcome.Summary;
                    if (!string.IsNullOrWhiteSpace(summaryLine))
                    {
                        lines.AppendLine(summaryLine.StartsWith("  •") ? summaryLine : $"  • {summaryLine}");
                    }
                }
            }
            else
            {
                foreach (var effect in evt.effects)
                {
                    var handler = EffectFactory.Create(effect.EffectName);
                    string summaryLine = handler != null
                        ? handler.GetSummary(effect)
                        : $"Unknown Effect: {effect.EffectName} ({effect.DefNameText})";

                    if (!string.IsNullOrWhiteSpace(summaryLine))
                    {
                        lines.AppendLine(summaryLine.StartsWith("  •") ? summaryLine : $"  • {summaryLine}");
                    }
                }
            }

            return lines.ToString().TrimEnd();
        }
    }
}