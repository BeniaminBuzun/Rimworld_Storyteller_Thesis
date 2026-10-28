using System;

namespace RimTelligence.Source.Memory.Search
{
    /// <summary>Embedding (de)serialization and similarity helpers shared by all stores.</summary>
    public static class RagBlob
    {
        public static byte[] ToBlob(float[] values)
        {
            var bytes = new byte[values.Length * sizeof(float)];
            Buffer.BlockCopy(values, 0, bytes, 0, bytes.Length);
            return bytes;
        }

        public static float[] FromBlob(byte[] bytes)
        {
            var values = new float[bytes.Length / sizeof(float)];
            Buffer.BlockCopy(bytes, 0, values, 0, values.Length * sizeof(float));
            return values;
        }

        /// <summary>
        /// Cosine similarity. Returns false when the vectors can't be compared (null or different
        /// dimensions, e.g. rows embedded with another model), so callers can skip the row.
        /// </summary>
        public static bool TryCosineSim(float[] vectorA, float[] vectorB, out float score)
        {
            score = 0f;
            if (vectorA == null || vectorB == null || vectorA.Length != vectorB.Length)
                return false;

            float dot = 0f, magA = 0f, magB = 0f;
            for (int i = 0; i < vectorA.Length; i++)
            {
                dot += vectorA[i] * vectorB[i];
                magA += vectorA[i] * vectorA[i];
                magB += vectorB[i] * vectorB[i];
            }

            score = dot / ((float)Math.Sqrt(magA) * (float)Math.Sqrt(magB) + 1e-8f);
            return true;
        }
    }
}
