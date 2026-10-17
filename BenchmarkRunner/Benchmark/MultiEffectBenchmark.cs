using RimTelligence.CustomEvents.EventSelector;
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
    /// Automated benchmark for testing several effects at once per prompt.
    /// Supports curated multi-effect archetypes, user-specified combos via CLI,
    /// dynamic heuristic storyteller draws, and partition/combination sets.
    /// </summary>
    public static class MultiEffectBenchmark
    {
        private const string ConfigFileName = "benchmark-config.json";
        private const string SnapshotFileName = "example-snapshot.json";

        public static async Task RunAsync(
            string configPath = null,
            string snapshotPath = null,
            string outputDirectory = null,
            string effectFilter = null,
            string comboFilter = null,
            int effectsPerPrompt = 0,
            int maxCases = 0,
            int heuristicDrawCount = 0,
            string preset = null)
        {
            string resolvedConfigPath = configPath ?? ModPaths.Resolve(Path.Combine("Data", ConfigFileName));
            string resolvedSnapshotPath = snapshotPath ?? ModPaths.Resolve(Path.Combine("Data", SnapshotFileName));

            string resolvedOutputDirectory = string.IsNullOrEmpty(outputDirectory)
                ? Path.Combine(ModPaths.Root, "BenchmarkOutput", "multi-effect")
                : (Path.IsPathRooted(outputDirectory)
                    ? outputDirectory
                    : Path.Combine(ModPaths.Root, outputDirectory));
            resolvedOutputDirectory = Path.GetFullPath(resolvedOutputDirectory);

            List<BenchmarkProvider> providers = BenchmarkInputs.LoadProviders(resolvedConfigPath);
            ColonySnapshot snapshot = BenchmarkInputs.LoadSnapshot(resolvedSnapshotPath);
            string narrative = SnapshotNarrator.Narrate(snapshot);

            Directory.CreateDirectory(resolvedOutputDirectory);
            string promptDirectory = Path.Combine(resolvedOutputDirectory, "prompts");
            Directory.CreateDirectory(promptDirectory);

            List<MultiEffectCase> cases = GenerateCases(
                snapshot,
                narrative,
                promptDirectory,
                comboFilter,
                effectFilter,
                effectsPerPrompt,
                maxCases,
                heuristicDrawCount,
                preset);

            if (cases.Count == 0)
                throw new InvalidOperationException("Multi-effect benchmark has no test cases to run.");

            ModLogger.Message($"[Benchmark][multi] Snapshot '{snapshot.colonyName}' -> {cases.Count} multi-effect prompts.");

            List<BenchmarkResult> results = new List<BenchmarkResult>();

            foreach (BenchmarkProvider provider in providers)
            {
                if (provider?.models == null || provider.models.Count == 0)
                {
                    ModLogger.Warning($"[Benchmark][multi] Provider '{provider?.name}' has no models; skipping.");
                    continue;
                }

                foreach (string model in provider.models)
                {
                    if (string.IsNullOrWhiteSpace(model))
                        continue;

                    ModLogger.Message($"[Benchmark][multi] === {provider.name} / {model} ===");

                    ProviderConfig providerConfig = ModelBenchmark.BuildProviderConfig(provider, model);
                    long warmUpMs = await ModelBenchmark.WarmUpAsync(providerConfig, model);

                    foreach (MultiEffectCase testCase in cases)
                    {
                        ModLogger.Message($"[Benchmark][multi] {model} x {testCase.CaseName} ({testCase.EffectCount} effects, {testCase.Prompt.Length} chars)");

                        BenchmarkResult result = await ModelBenchmark.RunPromptAsync(
                            providerConfig, provider.name, model, testCase.Prompt, testCase.Chosen, warmUpMs);
                        results.Add(result);

                        string retentionInfo = result.allCuedEffectsReturned
                            ? "all effects retained"
                            : $"{result.returnedEffectCount}/{result.expectedEffectCount} effects returned";

                        ModLogger.Message(result.success
                            ? $"[Benchmark][multi] PASS {model} x {testCase.CaseName} in {result.elapsedMs} ms ({retentionInfo})"
                            : $"[Benchmark][multi] FAIL {model} x {testCase.CaseName} in {result.elapsedMs} ms: {result.error}");
                    }
                }
            }

            BenchmarkReport.WriteMultiEffect(resolvedOutputDirectory, results);
        }

        private static List<MultiEffectCase> GenerateCases(
            ColonySnapshot snapshot,
            string narrative,
            string promptDirectory,
            string comboFilter,
            string effectFilter,
            int effectsPerPrompt,
            int maxCases,
            int heuristicDrawCount,
            string preset)
        {
            var cases = new List<MultiEffectCase>();
            HashSet<string> allowedEffects = ParseFilter(effectFilter);

            // Mode 1: Explicit user-specified combos (e.g. --combos "DamagePawn+AddDisease,ResourcePodCrash+MeteoriteImpact")
            if (!string.IsNullOrWhiteSpace(comboFilter))
            {
                string[] comboStrings = comboFilter.Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries);
                foreach (string comboStr in comboStrings)
                {
                    string[] parts = comboStr.Split(new[] { '+', '/', '&' }, StringSplitOptions.RemoveEmptyEntries)
                        .Select(p => p.Trim())
                        .Where(p => !string.IsNullOrEmpty(p))
                        .ToArray();

                    if (parts.Length < 2)
                    {
                        ModLogger.Warning($"[Benchmark][multi] Skipping combo '{comboStr}': requires at least 2 effects.");
                        continue;
                    }

                    var testCase = CreateCase(comboStr, parts, snapshot, narrative, promptDirectory);
                    if (testCase != null)
                        cases.Add(testCase);
                }

                return LimitCases(cases, maxCases);
            }

            // Mode 2: Heuristic storyteller draws (--heuristic <N>)
            if (heuristicDrawCount > 0)
            {
                for (int i = 0; i < heuristicDrawCount; i++)
                {
                    List<IEffect> drawn = HeuristicEventSelector.Select(snapshot);
                    if (drawn == null) drawn = new List<IEffect>();

                    // Ensure at least 2 effects for a multi-effect benchmark
                    if (drawn.Count < 2)
                    {
                        var pool = EffectFactory.GetAllEffectNames()
                            .Where(name => !drawn.Any(d => string.Equals(d.Config.EffectName, name, StringComparison.OrdinalIgnoreCase)))
                            .ToList();

                        if (pool.Count > 0)
                        {
                            string filler = pool[i % pool.Count];
                            var fillerEffect = EffectFactory.Create(filler);
                            if (fillerEffect != null)
                            {
                                if (filler == "ResourcePodCrash" || filler == "PawnFoundItems")
                                    fillerEffect.Config.RelatedDefList = ItemCategoryRegistry.DrawRandomCategory();
                                drawn.Add(fillerEffect);
                            }
                        }
                    }

                    string caseName = $"HeuristicDraw_{i + 1}_{string.Join("+", drawn.Select(e => e.Config.EffectName))}";
                    string prompt = ProperPromptGenerator.BuildSystemPrompt(drawn, snapshot);
                    string promptPath = Path.Combine(promptDirectory, $"multi-{SanitizeFileName(caseName)}.txt");
                    File.WriteAllText(promptPath, prompt);

                    cases.Add(new MultiEffectCase
                    {
                        CaseName = string.Join(" + ", drawn.Select(e => e.Config.EffectName)),
                        Description = $"Storyteller heuristic draw #{i + 1}",
                        Chosen = drawn,
                        Prompt = prompt
                    });
                }

                return LimitCases(cases, maxCases);
            }

            // Mode 3: Combinatorial chunking by size (--effects-per-prompt <N> or --size <N>)
            if (effectsPerPrompt > 1)
            {
                List<string> effectNames = EffectFactory.GetAllEffectNames()
                    .OrderBy(name => name, StringComparer.OrdinalIgnoreCase)
                    .ToList();

                if (allowedEffects.Count > 0)
                    effectNames = effectNames.Where(name => allowedEffects.Contains(name)).ToList();

                for (int i = 0; i < effectNames.Count; i += effectsPerPrompt)
                {
                    var chunk = effectNames.Skip(i).Take(effectsPerPrompt).ToList();
                    if (chunk.Count < 2)
                        break;

                    string name = string.Join("+", chunk);
                    var testCase = CreateCase(name, chunk, snapshot, narrative, promptDirectory);
                    if (testCase != null)
                        cases.Add(testCase);
                }

                return LimitCases(cases, maxCases);
            }

            // Mode 4: Curated archetypes (Default)
            var archetypes = GetCuratedArchetypes();

            foreach (var arch in archetypes)
            {
                if (effectsPerPrompt > 0 && arch.Effects.Length != effectsPerPrompt)
                    continue;

                if (allowedEffects.Count > 0 && !arch.Effects.All(e => allowedEffects.Contains(e)))
                    continue;

                var testCase = CreateCase(arch.Name, arch.Effects, snapshot, narrative, promptDirectory, arch.Description);
                if (testCase != null)
                    cases.Add(testCase);
            }

            return LimitCases(cases, maxCases);
        }

        private static List<MultiEffectCase> LimitCases(List<MultiEffectCase> cases, int maxCases)
        {
            if (maxCases > 0 && cases.Count > maxCases)
                return cases.Take(maxCases).ToList();
            return cases;
        }

        private static MultiEffectCase CreateCase(
            string label,
            IEnumerable<string> effectNames,
            ColonySnapshot snapshot,
            string narrative,
            string promptDirectory,
            string description = null)
        {
            var chosen = new List<IEffect>();
            foreach (string name in effectNames)
            {
                IEffect effect;
                try
                {
                    effect = EffectFactory.Create(name);
                }
                catch (Exception exception)
                {
                    ModLogger.Warning($"[Benchmark][multi] Skipping '{name}' in case '{label}': factory threw {exception.Message}");
                    continue;
                }

                if (effect == null)
                {
                    ModLogger.Warning($"[Benchmark][multi] Skipping '{name}' in case '{label}': unknown effect.");
                    continue;
                }

                if (name == "ResourcePodCrash" || name == "PawnFoundItems")
                {
                    effect.Config.RelatedDefList = ItemCategoryRegistry.DrawRandomCategory();
                }

                chosen.Add(effect);
            }

            if (chosen.Count < 2)
            {
                ModLogger.Warning($"[Benchmark][multi] Case '{label}' has fewer than 2 valid effects; skipping.");
                return null;
            }

            string prompt;
            try
            {
                prompt = ProperPromptGenerator.BuildSystemPrompt(chosen, snapshot);
            }
            catch (Exception exception)
            {
                ModLogger.Warning($"[Benchmark][multi] Skipping '{label}': prompt build failed: {exception.Message}");
                return null;
            }

            string caseName = string.Join(" + ", chosen.Select(e => e.Config.EffectName));
            string sanitized = SanitizeFileName(label);
            string promptPath = Path.Combine(promptDirectory, $"multi-{sanitized}.txt");
            File.WriteAllText(promptPath, prompt);

            return new MultiEffectCase
            {
                CaseName = caseName,
                Description = description ?? caseName,
                Chosen = chosen,
                Prompt = prompt
            };
        }

        private static string SanitizeFileName(string name)
        {
            char[] invalid = Path.GetInvalidFileNameChars();
            char[] cleaned = name.Select(c => invalid.Contains(c) || c == '+' || c == ' ' ? '_' : c).ToArray();
            string result = new string(cleaned);
            while (result.Contains("__"))
                result = result.Replace("__", "_");
            return result.Trim(new[] { '_' });
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

        private static List<ArchetypeDefinition> GetCuratedArchetypes()
        {
            return new List<ArchetypeDefinition>
            {
                // 2-Effect Pairs
                new ArchetypeDefinition("TraumaAndMood", new[] { "DamagePawn", "MoodDebuff" }, "Colonist physical damage combined with psychological distress"),
                new ArchetypeDefinition("BiologicalCrisis", new[] { "AddDisease", "DamagePawn" }, "Infection outbreak paired with traumatic colonist injury"),
                new ArchetypeDefinition("StormAndCropBlight", new[] { "StartExtremeWeatherHandler", "CropBlight" }, "Dangerous weather storm accompanied by crop blight"),
                new ArchetypeDefinition("MeteorAndSupplyDrop", new[] { "MeteoriteImpact", "ResourcePodCrash" }, "Meteorite impact paired with cargo pod arrival"),
                new ArchetypeDefinition("WildlifeIncident", new[] { "AnimalInsanity", "SpawnAnimals" }, "Sudden animal spawn wave accompanied by animal madness"),
                new ArchetypeDefinition("MiracleAndRelief", new[] { "MiracleHealDisease", "MoodBuff" }, "Miraculous healing paired with colony-wide mood surge"),
                new ArchetypeDefinition("ScavengeAndTame", new[] { "PawnFoundItems", "SelfTame" }, "Pawn finding items outside while a wild animal self-tames"),
                new ArchetypeDefinition("SkillAndInspiration", new[] { "AddExpPoints", "MoodBuff" }, "Skill breakthrough combined with positive inspiration"),
                new ArchetypeDefinition("WeatherShiftAndStorm", new[] { "ChangeWeather", "StartExtremeWeatherHandler" }, "Weather pattern change culminating in an extreme weather event"),
                new ArchetypeDefinition("RestorationMiracles", new[] { "MiracleHealChronicCondition", "MiracleHealMissingPart" }, "Dual healing miracle curing a chronic affliction and missing part"),

                // 3-Effect Trios
                new ArchetypeDefinition("CompoundCrisis", new[] { "DamagePawn", "AddDisease", "MoodDebuff" }, "Triple trauma: physical wound, disease contraction, and mood drop"),
                new ArchetypeDefinition("FortuneWindfall", new[] { "ResourcePodCrash", "PawnFoundItems", "SelfTame" }, "Triple boon: crash pods, scavenged loot, and animal join"),
                new ArchetypeDefinition("EcologicalCollapse", new[] { "StartExtremeWeatherHandler", "CropBlight", "AnimalInsanity" }, "Triple ecological crisis: storm, blighted food, and manhunting wildlife"),
                new ArchetypeDefinition("LossAndSetback", new[] { "LoseItems", "LoseExpPoints", "MoodDebuff" }, "Triple setback: stolen items, lost expertise, and morale collapse"),

                // 4-Effect Complexes
                new ArchetypeDefinition("ColonyCatastrophe", new[] { "DamagePawn", "LoseItems", "AddDisease", "StartExtremeWeatherHandler" }, "Quadruple disaster challenging colony survival on every front"),
                new ArchetypeDefinition("TrialAndTriumph", new[] { "ResourcePodCrash", "MoodBuff", "DamagePawn", "CropBlight" }, "Quadruple mixed event delivering both gifts and severe trials")
            };
        }

        private sealed class ArchetypeDefinition
        {
            public string Name { get; }
            public string[] Effects { get; }
            public string Description { get; }

            public ArchetypeDefinition(string name, string[] effects, string description)
            {
                Name = name;
                Effects = effects;
                Description = description;
            }
        }
    }

    public sealed class MultiEffectCase
    {
        public string CaseName;
        public string Description;
        public List<IEffect> Chosen;
        public string Prompt;
        public int EffectCount => Chosen?.Count ?? 0;
    }
}
