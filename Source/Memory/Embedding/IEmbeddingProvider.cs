using System.Threading.Tasks;

namespace RimTelligence.Source.Memory.Embedding
{
    public interface IEmbeddingProvider
    {
        float[] Embed(string text);
        Task<float[]> EmbedAsync(string text);
    }
}
