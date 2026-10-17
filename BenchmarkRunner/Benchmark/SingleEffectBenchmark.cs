using RimTelligence.CustomEvents.Handlers;
using RimTelligence.CustomEvents.Prompting;
using RimTelligence.DataSnapshots;
using RimTelligence.Llm;
using RimTelligence.Source.CustomEvents.Handlers;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace RimTelligence.Benchmark
{
    /// <summary>
    /// One prompt per effect: builds a single-effect prompt for every registered
    /// effect and runs it against every provider/model. Complements the default
    /// multi-effect <see cref="CustomEvents"/> benchmark.
    /// </summary>
    public static class SingleEffectBenchmark
    {
        private const string ConfigFileName = "benchmark-config.json";
        private const string SnapshotFileName = "example-snapshot.json";

        public static async Task RunAsync(
            string configPath = null,
            string snapshotPath = null,
            string outputDirectory = null,
            string effectFilter = null)
        {
            string resolvedConfigPath = configPath ?? ModPaths.Resolve(Path.Combine("Data", ConfigFileName));
            string resolvedSnapshotPath = snapshotPath ?? ModPaths.Resolve(Path.Combine("Data", SnapshotFileName));

            string resolvedOutputDirectory = string.IsNullOrEmpty(outputDirectory)
                ? Path.Combine(ModPaths.Root, "BenchmarkOutput", "single-effect")
                : (Path.IsPathRooted(outputDirectory)
                    ? outputDirectory
                    : Path.Combine(ModPaths.Root, outputDirectory));
            resolvedOutputDirectory = Path.GetFullPath(resolvedOutputDirectory);

            List<BenchmarkProvider> providers = BenchmarkInputs.LoadProviders(resolvedConfigPath);
            ColonySnapshot snapshot = BenchmarkInputs.LoadSnapshot(resolvedSnapshotPath);
            string narrative = SnapshotNarrator.Narrate(snapshot);

            List<string> effectNames = EffectFactory.GetAllEffectNames()
                .OrderBy(name => name, StringComparer.OrdinalIgnoreCase)
                .ToList();

            HashSet<string> filter = ParseFilter(effectFilter);
            if (filter.Count > 0)
                effectNames = effectNames.Where(name => filter.Contains(name)).ToList();

            if (effectNames.Count == 0)
                throw new InvalidOperationException("Single-effect benchmark has no effects to run.");

            Directory.CreateDirectory(resolvedOutputDirectory);
            string promptDirectory = Path.Combine(resolvedOutputDirectory, "prompts");
            Directory.CreateDirectory(promptDirectory);

            // Build one single-effect prompt per effect up front.
            var cases = new List<SingleEffectCase>();
            foreach (string effectName in effectNames)
            {
                IEffect effect;
                try
                {
                    effect = EffectFactory.Create(effectName);
                }
                catch (Exception exception)
                {
                    ModLogger.Warning($"[Benchmark][single] Skipping '{effectName}': factory threw: {exception.Message}");
                    continue;
                }

                if (effect == null)
                {
                    ModLogger.Warning($"[Benchmark][single] Skipping '{effectName}': unknown effect.");
                    continue;
                }

                var chosen = new List<IEffect> { effect };
                string prompt;
                try
                {
                    prompt = ProperPromptGenerator.BuildSystemPrompt(chosen, snapshot);
                }
                catch (Exception exception)
                {
                    ModLogger.Warning($"[Benchmark][single] Skipping '{effectName}': prompt build failed: {exception.Message}");
                    continue;
                }

                string promptPath = Path.Combine(promptDirectory, $"single-{effectName}.txt");
                File.WriteAllText(promptPath, prompt);
                cases.Add(new SingleEffectCase { EffectName = effectName, Prompt = prompt, Chosen = chosen });
            }

            ModLogger.Message($"[Benchmark][single] Snapshot '{snapshot.colonyName}' -> {cases.Count} single-effect prompts.");

            var results = new List<BenchmarkResult>();

            foreach (BenchmarkProvider provider in providers)
            {
                if (provider?.models == null || provider.models.Count == 0)
                {
                    ModLogger.Warning($"[Benchmark][single] Provider '{provider?.name}' has no models; skipping.");
                    continue;
                }

                foreach (string model in provider.models)
                {
                    if (string.IsNullOrWhiteSpace(model))
                        continue;

                    ModLogger.Message($"[Benchmark][single] === {provider.name} / {model} ===");

                    ProviderConfig providerConfig = ModelBenchmark.BuildProviderConfig(provider, model);
                    long warmUpMs = await ModelBenchmark.WarmUpAsync(providerConfig, model);

                    foreach (SingleEffectCase testCase in cases)
                    {
                        ModLogger.Message($"[Benchmark][single] {model} x {testCase.EffectName} ({testCase.Prompt.Length} chars)");

                        BenchmarkResult result = await ModelBenchmark.RunPromptAsync(
                            providerConfig, provider.name, model, testCase.Prompt, testCase.Chosen, warmUpMs);
                        results.Add(result);

                        ModLogger.Message(result.success
                            ? $"[Benchmark][single] PASS {model} x {testCase.EffectName} in {result.elapsedMs} ms"
                            : $"[Benchmark][single] FAIL {model} x {testCase.EffectName} in {result.elapsedMs} ms: {result.error}");
                    }
                }
            }

            BenchmarkReport.WriteSingleEffect(resolvedOutputDirectory, results);
        }

        private static HashSet<string> ParseFilter(string effectFilter)
        {
            var filter = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            if (string.IsNullOrWhiteSpace(effectFilter))
                return filter;

            foreach (string part in effectFilter.Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries))
            {
                string name = part.Trim();
                if (!string.IsNullOrEmpty(name))
                    filter.Add(name);
            }

            return filter;
        }

        private sealed class SingleEffectCase
        {
            public string EffectName;
            public string Prompt;
            public List<IEffect> Chosen;
        }
    }
}
