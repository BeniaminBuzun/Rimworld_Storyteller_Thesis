using System.Collections.Generic;

namespace RimTelligence.Benchmark
{
    internal class BenchmarkResult
    {
        public string providerName;
        public string providerId;
        public string endpoint;
        public string model;
        public long warmUpMs;
        public bool success;
        public long elapsedMs;
        public int promptTokens;
        public int completionTokens;
        public int reasoningTokens;
        public string letterTitle;
        public string letterText;
        public List<string> effects;
        public string rawResponse;
        public string error;
    }
}
