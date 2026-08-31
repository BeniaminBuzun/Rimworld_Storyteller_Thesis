using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RimTelligence.Memory
{
    public interface IEmbeddingProvider
    {
        float[] Embed(string text);
        Task<float[]> EmbedAsync(string text);
    }
}
