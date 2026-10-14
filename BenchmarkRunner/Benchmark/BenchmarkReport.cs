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
    }
}
