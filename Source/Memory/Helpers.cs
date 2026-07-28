using System;
using System.Collections.Generic;
using System.Linq;
using System.Data.SQLite;

namespace RimTelligence
{
   
    // Shared blob/similarity helpers used by both stores
    public static class RagBlob
    {
        public static byte[] ToBlob(float[] v)
        {
            var b = new byte[v.Length * 4];
            Buffer.BlockCopy(v, 0, b, 0, b.Length);
            return b;
        }

        public static float[] FromBlob(byte[] b)
        {
            var v = new float[b.Length / 4];
            Buffer.BlockCopy(b, 0, v, 0, b.Length);
            return v;
        }

        public static float CosineSim(float[] a, float[] b)
        {
            float dot = 0, ma = 0, mb = 0;
            for (int i = 0; i < a.Length; i++)
            {
                dot += a[i] * b[i];
                ma += a[i] * a[i];
                mb += b[i] * b[i];
            }
            return dot / ((float)Math.Sqrt(ma) * (float)Math.Sqrt(mb) + 1e-8f);
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