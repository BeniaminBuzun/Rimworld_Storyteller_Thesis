using System;

namespace RimTelligence.Source.Memory.Search
{
    /// <summary>Embedding serialization helpers for sqlite-vec.</summary>
    public static class RagBlob
    {
        public static byte[] ToBlob(float[] values)
        {
            if (values == null) return null;
            var bytes = new byte[values.Length * sizeof(float)];
            Buffer.BlockCopy(values, 0, bytes, 0, bytes.Length);
            return bytes;
        }
    }
}
