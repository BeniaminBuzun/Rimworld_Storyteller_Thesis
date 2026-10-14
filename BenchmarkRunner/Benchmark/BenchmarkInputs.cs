using Newtonsoft.Json;
using RimTelligence.DataSnapshots;
using System;
using System.Collections.Generic;
using System.IO;

namespace RimTelligence.Benchmark
{
    internal static class BenchmarkInputs
    {
        public static List<BenchmarkProvider> LoadProviders(string configPath)
        {
            if (!File.Exists(configPath))
                throw new FileNotFoundException("Benchmark config not found.", configPath);

            BenchmarkConfig config =
                JsonConvert.DeserializeObject<BenchmarkConfig>(File.ReadAllText(configPath));

            if (config?.providers == null || config.providers.Count == 0)
                throw new InvalidOperationException("Benchmark config contains no providers: " + configPath);

            return config.providers;
        }

        public static ColonySnapshot LoadSnapshot(string snapshotPath)
        {
            if (!File.Exists(snapshotPath))
                throw new FileNotFoundException("Snapshot not found.", snapshotPath);

            ColonySnapshot snapshot =
                JsonConvert.DeserializeObject<ColonySnapshot>(File.ReadAllText(snapshotPath));

            if (snapshot == null)
                throw new InvalidOperationException("Snapshot could not be parsed: " + snapshotPath);

            return snapshot;
        }
    }
}
