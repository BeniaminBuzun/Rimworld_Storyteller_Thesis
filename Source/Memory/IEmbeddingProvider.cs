using System.Threading.Tasks;

namespace RimTelligence.Memory
{
    public interface IEmbeddingProvider
    {
        float[] Embed(string text);
        Task<float[]> EmbedAsync(string text);
    }
}
