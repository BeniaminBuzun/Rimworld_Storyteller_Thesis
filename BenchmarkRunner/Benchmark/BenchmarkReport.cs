using Newtonsoft.Json;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace RimTelligence.Benchmark
{
    internal static class BenchmarkReport
    {
        public static void Write(string outputDirectory, List<BenchmarkResult> results)
        {
            string jsonPath = Path.Combine(outputDirectory, "benchmark-results.json");
            string markdownPath = Path.Combine(outputDirectory, "benchmark-results.md");

            File.WriteAllText(jsonPath, JsonConvert.SerializeObject(results, Formatting.Indented));
            File.WriteAllText(markdownPath, BuildMarkdown(results));

            ModLogger.Message($"[Benchmark] Wrote {jsonPath}");
            ModLogger.Message($"[Benchmark] Wrote {markdownPath}");
            ModLogger.Message(BuildSummary(results));
        }

        public static void WriteSingleEffect(string outputDirectory, List<BenchmarkResult> results)
        {
            string jsonPath = Path.Combine(outputDirectory, "single-effect-results.json");
            string markdownPath = Path.Combine(outputDirectory, "single-effect-results.md");

            File.WriteAllText(jsonPath, JsonConvert.SerializeObject(results, Formatting.Indented));
            File.WriteAllText(markdownPath, BuildSingleEffectMarkdown(results));

            ModLogger.Message($"[Benchmark][single] Wrote {jsonPath}");
            ModLogger.Message($"[Benchmark][single] Wrote {markdownPath}");
            ModLogger.Message(BuildSingleEffectSummary(results));
        }

        public static void WriteMultiEffect(string outputDirectory, List<BenchmarkResult> results)
        {
            string jsonPath = Path.Combine(outputDirectory, "multi-effect-results.json");
            string markdownPath = Path.Combine(outputDirectory, "multi-effect-results.md");

            File.WriteAllText(jsonPath, JsonConvert.SerializeObject(results, Formatting.Indented));
            File.WriteAllText(markdownPath, BuildMultiEffectMarkdown(results));

            ModLogger.Message($"[Benchmark][multi] Wrote {jsonPath}");
            ModLogger.Message($"[Benchmark][multi] Wrote {markdownPath}");
            ModLogger.Message(BuildMultiEffectSummary(results));
        }

        private static string BuildMultiEffectSummary(List<BenchmarkResult> results)
        {
            int passed = results.Count(result => result.success);
            int fullyRetained = results.Count(result => result.success && result.allCuedEffectsReturned);
            return $"[Benchmark][multi] Done: {passed}/{results.Count} (model x combo) produced valid events. Full retention (all cued effects present): {fullyRetained}/{results.Count}.";
        }

        private static string BuildMultiEffectMarkdown(List<BenchmarkResult> results)
        {
            StringBuilder builder = new StringBuilder();
            builder.AppendLine("# Multi-Effect Benchmark (several effects per prompt)");
            builder.AppendLine();

            int total = results.Count;
            int passed = results.Count(r => r.success);
            int fullyRetained = results.Count(r => r.success && r.allCuedEffectsReturned);
            double passPct = total > 0 ? (passed * 100.0 / total) : 0;
            double retainPct = total > 0 ? (fullyRetained * 100.0 / total) : 0;

            builder.AppendLine("## Overview");
            builder.AppendLine();
            builder.AppendLine($"- **Total Prompts Executed**: {total}");
            builder.AppendLine($"- **Valid Events**: {passed}/{total} ({passPct:F1}%)");
            builder.AppendLine($"- **All Cued Effects Retained**: {fullyRetained}/{total} ({retainPct:F1}%)");
            builder.AppendLine();

            builder.AppendLine("## Performance by Model");
            builder.AppendLine();
            builder.AppendLine("| Model | Passed | Total | Pass Rate | All Effects Retained | Retention Rate | Avg Latency (ms) |");
            builder.AppendLine("|---|---:|---:|---:|---:|---:|---:|");
            foreach (var group in results.GroupBy(r => r.model ?? string.Empty).OrderBy(g => g.Key))
            {
                int mTotal = group.Count();
                int mPassed = group.Count(r => r.success);
                int mRetained = group.Count(r => r.success && r.allCuedEffectsReturned);
                double mPassPct = mTotal > 0 ? (mPassed * 100.0 / mTotal) : 0;
                double mRetainPct = mTotal > 0 ? (mRetained * 100.0 / mTotal) : 0;
                double avgMs = mPassed > 0 ? group.Where(r => r.success).Average(r => r.elapsedMs) : 0;

                builder.Append("| ").Append(Escape(group.Key)).Append(" | ")
                    .Append(mPassed).Append(" | ")
                    .Append(mTotal).Append(" | ")
                    .Append($"{mPassPct:F1}%").Append(" | ")
                    .Append(mRetained).Append(" | ")
                    .Append($"{mRetainPct:F1}%").Append(" | ")
                    .Append($"{(long)avgMs}").AppendLine(" |");
            }
            builder.AppendLine();

            builder.AppendLine("## Performance by Effect Count");
            builder.AppendLine();
            builder.AppendLine("| Cued Effects | Passed | Total | Pass Rate | All Effects Retained | Retention Rate |");
            builder.AppendLine("|---:|---:|---:|---:|---:|---:|");
            foreach (var group in results.GroupBy(r => r.expectedEffectCount).OrderBy(g => g.Key))
            {
                int cTotal = group.Count();
                int cPassed = group.Count(r => r.success);
                int cRetained = group.Count(r => r.success && r.allCuedEffectsReturned);
                double cPassPct = cTotal > 0 ? (cPassed * 100.0 / cTotal) : 0;
                double cRetainPct = cTotal > 0 ? (cRetained * 100.0 / cTotal) : 0;

                builder.Append("| ").Append(group.Key).Append(" effects | ")
                    .Append(cPassed).Append(" | ")
                    .Append(cTotal).Append(" | ")
                    .Append($"{cPassPct:F1}%").Append(" | ")
                    .Append(cRetained).Append(" | ")
                    .Append($"{cRetainPct:F1}%").AppendLine(" |");
            }
            builder.AppendLine();

            builder.AppendLine("## Performance by Combination");
            builder.AppendLine();
            builder.AppendLine("| Combination | Cued | Passed | Total | Retention Rate |");
            builder.AppendLine("|---|---:|---:|---:|---:|");
            foreach (var group in results.GroupBy(r => r.cueEffect ?? string.Empty).OrderBy(g => g.Key))
            {
                int comboTotal = group.Count();
                int comboPassed = group.Count(r => r.success);
                int comboRetained = group.Count(r => r.success && r.allCuedEffectsReturned);
                int expectedCount = group.First().expectedEffectCount;
                double comboRetainPct = comboTotal > 0 ? (comboRetained * 100.0 / comboTotal) : 0;

                builder.Append("| ").Append(Escape(group.Key)).Append(" | ")
                    .Append(expectedCount).Append(" | ")
                    .Append(comboPassed).Append(" | ")
                    .Append(comboTotal).Append(" | ")
                    .Append($"{comboRetainPct:F1}%").AppendLine(" |");
            }
            builder.AppendLine();

            builder.AppendLine("## Detailed Results");
            builder.AppendLine();
            builder.AppendLine("| Provider | Model | Combination | Cued | Returned | Retained All? | Result | Load (ms) | Run (ms) | Tokens (P/C/R) | Returned Effects | Error |");
            builder.AppendLine("|---|---|---|---:|---:|---|---|---:|---:|---|---|---|");

            foreach (BenchmarkResult result in results.OrderBy(r => r.model).ThenBy(r => r.expectedEffectCount).ThenBy(r => r.cueEffect))
            {
                string tokenSummary = $"{result.promptTokens}/{result.completionTokens}/{result.reasoningTokens}";
                string allRetainedStr = result.success ? (result.allCuedEffectsReturned ? "YES" : "NO") : "-";

                builder.Append("| ")
                    .Append(Escape(result.providerName)).Append(" | ")
                    .Append(Escape(result.model)).Append(" | ")
                    .Append(Escape(result.cueEffect)).Append(" | ")
                    .Append(result.expectedEffectCount).Append(" | ")
                    .Append(result.returnedEffectCount).Append(" | ")
                    .Append(allRetainedStr).Append(" | ")
                    .Append(result.success ? "PASS" : "FAIL").Append(" | ")
                    .Append(result.warmUpMs).Append(" | ")
                    .Append(result.elapsedMs).Append(" | ")
                    .Append(tokenSummary).Append(" | ")
                    .Append(Escape(result.effects == null ? "" : string.Join(", ", result.effects))).Append(" | ")
                    .Append(Escape(result.error)).AppendLine(" |");
            }

            var successful = results.Where(r => r.success).ToList();
            if (successful.Count > 0)
            {
                builder.AppendLine();
                builder.AppendLine("## Sample Generated Stories");
                foreach (BenchmarkResult result in successful)
                {
                    builder.AppendLine();
                    builder.Append("### ").Append(Escape(result.model)).Append(" — ").Append(Escape(result.cueEffect)).AppendLine();
                    builder.Append("**").Append(Escape(result.letterTitle)).AppendLine("**");
                    builder.AppendLine();
                    builder.AppendLine(Escape(result.letterText));
                    builder.AppendLine();
                    builder.Append("*Returned effects: ").Append(Escape(result.effects == null ? "none" : string.Join(", ", result.effects))).AppendLine("*");
                }
            }

            return builder.ToString();
        }

        private static string BuildSummary(List<BenchmarkResult> results)
        {
            int passed = results.Count(result => result.success);
            return $"[Benchmark] Done: {passed}/{results.Count} models produced a valid event.";
        }

        private static string BuildMarkdown(List<BenchmarkResult> results)
        {
            StringBuilder builder = new StringBuilder();
            builder.AppendLine("# Model Benchmark");
            builder.AppendLine();
            builder.AppendLine("| Provider | Model | Result | Load (ms) | Run (ms) | Prompt | Completion | Reasoning | Effects | Error |");
            builder.AppendLine("|---|---|---|---:|---:|---:|---:|---:|---|---|");

            foreach (BenchmarkResult result in results)
            {
                builder.Append("| ")
                    .Append(Escape(result.providerName)).Append(" | ")
                    .Append(Escape(result.model)).Append(" | ")
                    .Append(result.success ? "PASS" : "FAIL").Append(" | ")
                    .Append(result.warmUpMs).Append(" | ")
                    .Append(result.elapsedMs).Append(" | ")
                    .Append(result.promptTokens).Append(" | ")
                    .Append(result.completionTokens).Append(" | ")
                    .Append(result.reasoningTokens).Append(" | ")
                    .Append(Escape(result.effects == null ? "" : string.Join(", ", result.effects))).Append(" | ")
                    .Append(Escape(result.error)).AppendLine(" |");
            }

            foreach (BenchmarkResult result in results.Where(result => result.success))
            {
                builder.AppendLine();
                builder.Append("## ").Append(result.model).AppendLine();
                builder.Append("**").Append(Escape(result.letterTitle)).AppendLine("**");
                builder.AppendLine();
                builder.AppendLine(Escape(result.letterText));
                builder.AppendLine();
                builder.AppendLine(Escape(string.Join(", ", result.effects)));
                builder.AppendLine();
            }

            return builder.ToString();
        }

        private static string Escape(string value)
        {
            return (value ?? string.Empty).Replace("|", "\\|").Replace("\r", " ").Replace("\n", " ");
        }

        private static string BuildSingleEffectSummary(List<BenchmarkResult> results)
        {
            int passed = results.Count(result => result.success);
            return $"[Benchmark][single] Done: {passed}/{results.Count} (model x effect) prompts produced a valid event.";
        }

        private static string BuildSingleEffectMarkdown(List<BenchmarkResult> results)
        {
            StringBuilder builder = new StringBuilder();
            builder.AppendLine("# Single-Effect Benchmark (one prompt per effect)");
            builder.AppendLine();
            builder.AppendLine("| Provider | Model | Cue Effect | Result | Load (ms) | Run (ms) | Prompt | Completion | Reasoning | Effects | Error |");
            builder.AppendLine("|---|---|---|---|---:|---:|---:|---:|---:|---|---|");

            foreach (BenchmarkResult result in results.OrderBy(r => r.model).ThenBy(r => r.cueEffect))
            {
                builder.Append("| ")
                    .Append(Escape(result.providerName)).Append(" | ")
                    .Append(Escape(result.model)).Append(" | ")
                    .Append(Escape(result.cueEffect)).Append(" | ")
                    .Append(result.success ? "PASS" : "FAIL").Append(" | ")
                    .Append(result.warmUpMs).Append(" | ")
                    .Append(result.elapsedMs).Append(" | ")
                    .Append(result.promptTokens).Append(" | ")
                    .Append(result.completionTokens).Append(" | ")
                    .Append(result.reasoningTokens).Append(" | ")
                    .Append(Escape(result.effects == null ? "" : string.Join(", ", result.effects))).Append(" | ")
                    .Append(Escape(result.error)).AppendLine(" |");
            }

            builder.AppendLine();
            builder.AppendLine("## Pass rate per effect");
            builder.AppendLine();
            builder.AppendLine("| Effect | PASS | Total |");
            builder.AppendLine("|---|---:|---:|");
            foreach (var group in results.GroupBy(r => r.cueEffect ?? string.Empty).OrderBy(g => g.Key))
            {
                int passed = group.Count(r => r.success);
                builder.Append("| ").Append(Escape(group.Key))
                    .Append(" | ").Append(passed)
                    .Append(" | ").Append(group.Count()).AppendLine(" |");
            }

            builder.AppendLine();
            builder.AppendLine("## Pass rate per model");
            builder.AppendLine();
            builder.AppendLine("| Model | PASS | Total |");
            builder.AppendLine("|---|---:|---:|");
            foreach (var group in results.GroupBy(r => r.model ?? string.Empty).OrderBy(g => g.Key))
            {
                int passed = group.Count(r => r.success);
                builder.Append("| ").Append(Escape(group.Key))
                    .Append(" | ").Append(passed)
                    .Append(" | ").Append(group.Count()).AppendLine(" |");
            }

            return builder.ToString();
        }
    }
}
