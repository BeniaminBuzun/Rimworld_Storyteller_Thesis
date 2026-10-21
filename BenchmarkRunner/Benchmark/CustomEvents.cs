using RimTelligence.CustomEvents.EventSelector;
using RimTelligence.CustomEvents.Handlers;
using RimTelligence.CustomEvents.Prompting;
using RimTelligence.DataSnapshots;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace RimTelligence.Benchmark
{
    public static class CustomEvents
    {
        private const string ConfigFileName = "benchmark-config.json";
        private const string SnapshotFileName = "example-snapshot.json";

        public static async Task RunAsync(
            string configPath = null,
            string snapshotPath = null,
            string outputDirectory = null,
            int repeatCount = 1)
        {
            string resolvedConfigPath = configPath ?? ModPaths.Resolve(Path.Combine("Data", ConfigFileName));
            string resolvedSnapshotPath = snapshotPath ?? ModPaths.Resolve(Path.Combine("Data", SnapshotFileName));

            string resolvedOutputDirectory = string.IsNullOrEmpty(outputDirectory)
                ? Path.Combine(ModPaths.Root, "BenchmarkOutput")
                : (Path.IsPathRooted(outputDirectory)
                    ? outputDirectory
                    : Path.Combine(ModPaths.Root, outputDirectory));
            resolvedOutputDirectory = Path.GetFullPath(resolvedOutputDirectory);

            List<BenchmarkProvider> providers = BenchmarkInputs.LoadProviders(resolvedConfigPath);
            ColonySnapshot snapshot = BenchmarkInputs.LoadSnapshot(resolvedSnapshotPath);

            List<IEffect> chosenEvents = HeuristicEventSelector.Select(snapshot);
            string narrative = SnapshotNarrator.Narrate(snapshot);
            string prompt = ProperPromptGenerator.BuildSystemPrompt(chosenEvents, snapshot);

            ModLogger.Message($"[Benchmark] Snapshot '{snapshot.colonyName}' -> prompt of {prompt.Length} chars.");
            ModLogger.Message($"[Benchmark] Cue: {string.Join(", ", chosenEvents.Select(effect => effect.Config.EffectName))}");
            if (repeatCount > 1)
                ModLogger.Message($"[Benchmark] Repeating each model {repeatCount} time(s) with the same prompt.");

            Directory.CreateDirectory(resolvedOutputDirectory);
            File.WriteAllText(Path.Combine(resolvedOutputDirectory, "benchmark-prompt.txt"), prompt);

            List<BenchmarkResult> results = new List<BenchmarkResult>();

            foreach (BenchmarkProvider provider in providers)
            {
                if (provider?.models == null || provider.models.Count == 0)
                {
                    ModLogger.Warning($"[Benchmark] Provider '{provider?.name}' has no models; skipping.");
                    continue;
                }

                foreach (string model in provider.models)
                {
                    if (string.IsNullOrWhiteSpace(model))
                        continue;

                    for (int run = 1; run <= repeatCount; run++)
                    {
                        string runLabel = repeatCount > 1 ? $" (run {run}/{repeatCount})" : "";
                        ModLogger.Message($"[Benchmark] === {provider.name} / {model}{runLabel} ===");

                        BenchmarkResult result =
                            await ModelBenchmark.RunAsync(provider, model, prompt, chosenEvents);
                        result.run = run;
                        results.Add(result);

                        ModLogger.Message(result.success
                            ? $"[Benchmark] PASS {model}{runLabel} in {result.elapsedMs} ms (P={result.promptTokens} C={result.completionTokens} R={result.reasoningTokens})"
                            : $"[Benchmark] FAIL {model}{runLabel} in {result.elapsedMs} ms: {result.error}");
                    }
                }
            }

            BenchmarkReport.Write(resolvedOutputDirectory, results);
        }
    }
}
