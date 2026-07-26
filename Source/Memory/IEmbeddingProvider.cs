using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RimTelligence.Source.Memory
{
    public interface IEmbeddingProvider
    {
        float[] Embed(string text);
    }

    // Placeholder — replace with a real call (e.g. LlmTornado embeddings endpoint,
    // OpenAI API, or a local embedding server) once you've decided on a source.
    // Returns a deterministic pseudo-embedding so the pipeline is testable end-to-end.
    public class StubEmbeddingProvider : IEmbeddingProvider
    {
        private const int Dim = 32;

        public float[] Embed(string text)
        {
            var vec = new float[Dim];
            if (string.IsNullOrEmpty(text)) return vec;

            int hash = text.GetHashCode();
            var rng = new System.Random(hash);
            for (int i = 0; i < Dim; i++)
                vec[i] = (float)(rng.NextDouble() * 2.0 - 1.0);

            return vec;
        }
    }
}
