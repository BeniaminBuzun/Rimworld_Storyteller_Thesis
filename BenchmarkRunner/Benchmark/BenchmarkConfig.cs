using System.Collections.Generic;

namespace RimTelligence.Benchmark
{
    internal class BenchmarkConfig
    {
        public List<BenchmarkProvider> providers { get; set; } = new List<BenchmarkProvider>();
    }

    internal class BenchmarkProvider
    {
        public string name { get; set; }
        public string providerId { get; set; }
        public string endpoint { get; set; }
        public string apiKey { get; set; }
        public List<string> models { get; set; } = new List<string>();
    }
}
