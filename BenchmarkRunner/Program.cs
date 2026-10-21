using System;
using System.IO;
using System.Linq;
using System.Reflection;

namespace RimTelligence.Benchmark.Runner
{
    public static class Program
    {
        public static int Main(string[] args)
        {
            try
            {
                string modRoot = FindModRoot();
                string managedDirectory = FindManagedDirectory(modRoot);

                AppDomain.CurrentDomain.AssemblyResolve += (_, e) => LoadGameAssembly(managedDirectory, e.Name);
                ModLogger.Sink = Console.WriteLine;   // Verse.Log is unusable headless.
                ModPaths.Initialize(modRoot);

                string benchmark = (Arg(args, "--benchmark") ?? Arg(args, "--mode") ?? "custom-events").Trim().ToLowerInvariant();
                string configPath = Arg(args, "--config");
                string snapshotPath = Arg(args, "--snapshot");
                string outputDirectory = Arg(args, "--out");
                string effectFilter = Arg(args, "--effects") ?? Arg(args, "--effect");
                string comboFilter = Arg(args, "--combos") ?? Arg(args, "--combo");
                string preset = Arg(args, "--preset") ?? Arg(args, "--suite");
                int.TryParse(Arg(args, "--effects-per-prompt") ?? Arg(args, "--effects-count") ?? Arg(args, "--size"), out int effectsPerPrompt);
                int.TryParse(Arg(args, "--cases") ?? Arg(args, "--count") ?? Arg(args, "--max-cases"), out int maxCases);
                int.TryParse(Arg(args, "--heuristic") ?? Arg(args, "--heuristic-draws"), out int heuristicDraws);

                if (benchmark == "single" || benchmark == "single-effect" || benchmark == "single-effects"
                    || benchmark == "every-effect" || benchmark == "effects" || benchmark == "per-effect")
                {
                    SingleEffectBenchmark.RunAsync(
                        outputDirectory: outputDirectory,
                        configPath: configPath,
                        snapshotPath: snapshotPath,
                        effectFilter: effectFilter).GetAwaiter().GetResult();
                }
                else if (benchmark == "multi" || benchmark == "multi-effect" || benchmark == "multi-effects"
                    || benchmark == "several" || benchmark == "several-effects" || benchmark == "combos" || benchmark == "combinations")
                {
                    MultiEffectBenchmark.RunAsync(
                        outputDirectory: outputDirectory,
                        configPath: configPath,
                        snapshotPath: snapshotPath,
                        effectFilter: effectFilter,
                        comboFilter: comboFilter,
                        effectsPerPrompt: effectsPerPrompt,
                        maxCases: maxCases,
                        heuristicDrawCount: heuristicDraws,
                        preset: preset).GetAwaiter().GetResult();
                }
                else
                {
                    CustomEvents.RunAsync(
                        outputDirectory: outputDirectory,
                        configPath: configPath,
                        snapshotPath: snapshotPath).GetAwaiter().GetResult();
                }

                return 0;
            }
            catch (Exception exception)
            {
                Console.Error.WriteLine("Benchmark failed: " + exception);
                return 1;
            }
        }

        private static string Arg(string[] args, string name)
        {
            int index = Array.IndexOf(args, name);
            return index >= 0 && index + 1 < args.Length ? args[index + 1] : null;
        }

        private static string FindModRoot()
        {
            for (DirectoryInfo directory = new DirectoryInfo(AppContext.BaseDirectory);
                 directory != null;
                 directory = directory.Parent)
            {
                if (Directory.Exists(Path.Combine(directory.FullName, "About")))
                    return directory.FullName;
            }

            throw new DirectoryNotFoundException("Could not find the mod root above the exe.");
        }

        private static string FindManagedDirectory(string modRoot)
        {
            DirectoryInfo gameRoot = Directory.GetParent(modRoot)?.Parent;
            if (gameRoot == null)
                throw new DirectoryNotFoundException("Unexpected mod layout: " + modRoot);

            string managed = Directory.GetDirectories(gameRoot.FullName, "*_Data")
                .Select(data => Path.Combine(data, "Managed"))
                .FirstOrDefault(candidate => File.Exists(Path.Combine(candidate, "Assembly-CSharp.dll")));

            return managed ?? throw new DirectoryNotFoundException(
                "Could not find the RimWorld Managed folder above " + modRoot + ".");
        }

        private static Assembly LoadGameAssembly(string managedDirectory, string assemblyName)
        {
            string path = Path.Combine(managedDirectory, new AssemblyName(assemblyName).Name + ".dll");
            return File.Exists(path) ? Assembly.LoadFrom(path) : null;
        }
    }
}
