using Newtonsoft.Json;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace RimTelligence.Benchmark
{
    internal static class BenchmarkReport
    {
        // ------------------------------------------------------------------
        // Entry points
        // ------------------------------------------------------------------

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

        // ------------------------------------------------------------------
        // Console summaries
        // ------------------------------------------------------------------

        private static string BuildSummary(List<BenchmarkResult> results)
        {
            int passed = results.Count(result => result.success);
            return $"[Benchmark] Done: {passed}/{results.Count} models produced a valid event.";
        }

        private static string BuildSingleEffectSummary(List<BenchmarkResult> results)
        {
            int passed = results.Count(result => result.success);
            return $"[Benchmark][single] Done: {passed}/{results.Count} (model x effect) prompts produced a valid event.";
        }

        private static string BuildMultiEffectSummary(List<BenchmarkResult> results)
        {
            int passed = results.Count(result => result.success);
            int fullyRetained = results.Count(result => result.success && result.allCuedEffectsReturned);
            return $"[Benchmark][multi] Done: {passed}/{results.Count} (model x combo) produced valid events. Full retention (all cued effects present): {fullyRetained}/{results.Count}.";
        }

        // ------------------------------------------------------------------
        // Markdown builders (tables only, same schema for all three reports)
        //   1. Requested Effects
        //   2. Detailed Results
        //   3. Summary tables
        // ------------------------------------------------------------------

        private static string BuildMarkdown(List<BenchmarkResult> results)
        {
            // Legacy single-prompt benchmark: no cue column, no per-effect summaries.
            return BuildTables(results, cueLabel: null, includeEffectCountSummary: false);
        }

        private static string BuildSingleEffectMarkdown(List<BenchmarkResult> results)
        {
            return BuildTables(results, cueLabel: "Effect", includeEffectCountSummary: false);
        }

        private static string BuildMultiEffectMarkdown(List<BenchmarkResult> results)
        {
            return BuildTables(results, cueLabel: "Combination", includeEffectCountSummary: true);
        }

        private static string BuildTables(List<BenchmarkResult> results, string cueLabel, bool includeEffectCountSummary)
        {
            StringBuilder builder = new StringBuilder();
            bool showCue = cueLabel != null;

            // ---------- 1. Requested effects ----------
            bool anyCue = results.Any(r => !string.IsNullOrEmpty(r.cueEffect));
            if (showCue && anyCue)
            {
                builder.AppendLine("## Requested Effects");
                builder.AppendLine();
                builder.AppendLine($"| {cueLabel} | Cued Effects (count) |");
                builder.AppendLine("|---|---:|");
                foreach (var group in results.GroupBy(r => r.cueEffect ?? string.Empty).OrderBy(g => g.Key))
                {
                    builder.Append("| ").Append(Escape(group.Key)).Append(" | ")
                        .Append(group.First().expectedEffectCount).AppendLine(" |");
                }
                builder.AppendLine();
            }

            // ---------- 2. Detailed results ----------
            // Full Event        = valid event AND every cued effect survived validation
            // Passed Validation = valid event, but validation dropped some cued effects
            builder.AppendLine("## Detailed Results");
            builder.AppendLine();

            StringBuilder header = new StringBuilder("| Model | ");
            StringBuilder divider = new StringBuilder("|---|");
            if (showCue)
            {
                header.Append(cueLabel).Append(" | ");
                divider.Append("---|");
            }
            header.Append("Run | Cued | Returned | Full Event | Passed Validation | LetterTitle | LetterText | Effects | Run (ms) | Error |");
            divider.Append("---:|---:|---:|---|---|---|---|---|---:|---|");
            builder.AppendLine(header.ToString());
            builder.AppendLine(divider.ToString());

            foreach (BenchmarkResult r in results
                .OrderBy(x => x.model)
                .ThenBy(x => x.expectedEffectCount)
                .ThenBy(x => x.cueEffect)
                .ThenBy(x => x.run))
            {
                bool fullEvent = IsFullEvent(r);
                bool partial = r.success && !fullEvent;

                builder.Append("| ").Append(Escape(r.model)).Append(" | ");
                if (showCue)
                    builder.Append(Escape(r.cueEffect)).Append(" | ");

                builder
                    .Append(r.run).Append(" | ")
                    .Append(r.expectedEffectCount).Append(" | ")
                    .Append(r.returnedEffectCount).Append(" | ")
                    .Append(fullEvent ? "YES" : "NO").Append(" | ")
                    .Append(partial ? "YES (effects dropped)" : "-").Append(" | ")
                    .Append(r.success ? Escape(r.letterTitle) : "").Append(" | ")
                    .Append(r.success ? Escape(r.letterText) : "").Append(" | ")
                    .Append(Escape(r.effects == null ? "" : string.Join(", ", r.effects))).Append(" | ")
                    .Append(r.elapsedMs).Append(" | ")
                    .Append(Escape(r.error)).AppendLine(" |");
            }
            builder.AppendLine();

            // ---------- 3. Summary tables ----------
            AppendSummaryTable(builder, "Summary by Model", "Model",
                results.GroupBy(r => r.model ?? string.Empty)
                    .OrderBy(g => g.Key)
                    .Select(g => new KeyValuePair<string, List<BenchmarkResult>>(g.Key, g.ToList())));

            if (includeEffectCountSummary)
            {
                AppendSummaryTable(builder, "Summary by Effect Count", "Cued Effects",
                    results.GroupBy(r => r.expectedEffectCount)
                        .OrderBy(g => g.Key)
                        .Select(g => new KeyValuePair<string, List<BenchmarkResult>>(g.Key.ToString(), g.ToList())));
            }

            if (showCue && anyCue)
            {
                AppendSummaryTable(builder, $"Summary by {cueLabel}", cueLabel,
                    results.GroupBy(r => r.cueEffect ?? string.Empty)
                        .OrderBy(g => g.Key)
                        .Select(g => new KeyValuePair<string, List<BenchmarkResult>>(g.Key, g.ToList())));
            }

            AppendSummaryTable(builder, "Summary Overall", "Scope",
                new[] { new KeyValuePair<string, List<BenchmarkResult>>("All", results) });

            return builder.ToString();
        }

        // ------------------------------------------------------------------
        // Helpers
        // ------------------------------------------------------------------

        // A "full event" is a valid event where nothing cued was lost.
        // If no effects were cued (expectedEffectCount <= 0) a valid event counts as full.
        private static bool IsFullEvent(BenchmarkResult r)
        {
            return r.success && (r.expectedEffectCount <= 0 || r.allCuedEffectsReturned);
        }

        private static void AppendSummaryTable(
            StringBuilder builder,
            string title,
            string firstColumn,
            IEnumerable<KeyValuePair<string, List<BenchmarkResult>>> groups)
        {
            builder.AppendLine($"## {title}");
            builder.AppendLine();
            builder.AppendLine($"| {firstColumn} | Total | Full Event | Full Event % | Passed Validation (effects dropped) | Failed | Avg Latency (ms) |");
            builder.AppendLine("|---|---:|---:|---:|---:|---:|---:|");
            foreach (var group in groups)
            {
                AppendSummaryRow(builder, group.Key, group.Value);
            }
            builder.AppendLine();
        }

        private static void AppendSummaryRow(StringBuilder builder, string label, List<BenchmarkResult> rows)
        {
            int total = rows.Count;
            int full = rows.Count(IsFullEvent);
            int partial = rows.Count(r => r.success && !IsFullEvent(r));
            int failed = rows.Count(r => !r.success);
            double fullPct = total > 0 ? full * 100.0 / total : 0;
            var ok = rows.Where(r => r.success).ToList();
            long avgMs = ok.Count > 0 ? (long)ok.Average(r => r.elapsedMs) : 0;

            builder.Append("| ").Append(Escape(label)).Append(" | ")
                .Append(total).Append(" | ")
                .Append(full).Append(" | ")
                .Append($"{fullPct:F1}%").Append(" | ")
                .Append(partial).Append(" | ")
                .Append(failed).Append(" | ")
                .Append(avgMs).AppendLine(" |");
        }

        private static string Escape(string value)
        {
            return (value ?? string.Empty).Replace("|", "\\|").Replace("\r", " ").Replace("\n", " ");
        }
    }
}