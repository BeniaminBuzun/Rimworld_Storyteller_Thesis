using System;

namespace RimTelligence.Memory
{

    // Shared blob/similarity helpers used by both stores
    public static class RagBlob
    {
        public static byte[] ToBlob(float[] values)
        {
            var bytes = new byte[values.Length * 4];
            Buffer.BlockCopy(values, 0, bytes, 0, bytes.Length);
            return bytes;
        }

        public static float[] FromBlob(byte[] bytes)
        {
            var values = new float[bytes.Length / 4];
            Buffer.BlockCopy(bytes, 0, values, 0, bytes.Length);
            return values;
        }

        public static float CosineSim(float[] vectorA, float[] vectorB)
        {
            float dotProduct = 0, magnitudeA = 0, magnitudeB = 0;
            for (int index = 0; index < vectorA.Length; index++)
            {
                dotProduct += vectorA[index] * vectorB[index];
                magnitudeA += vectorA[index] * vectorA[index];
                magnitudeB += vectorB[index] * vectorB[index];
            }
            return dotProduct / ((float)Math.Sqrt(magnitudeA) * (float)Math.Sqrt(magnitudeB) + 1e-8f);
        }
    }

    public class RagResult
    {
        public string Text;
        public string Source;
        public float Score;
        public string DefName;
        public string Metadata;
    }
}