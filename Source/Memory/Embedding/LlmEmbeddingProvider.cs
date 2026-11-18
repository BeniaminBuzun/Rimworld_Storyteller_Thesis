using LlmTornado;
using LlmTornado.Code;
using LlmTornado.Embedding.Models;
using RimTelligence.AIStoryteller;
using RimTelligence.Llm;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace RimTelligence.Source.Memory.Embedding
{
    public class LlmEmbeddingProvider : IEmbeddingProvider, IDisposable
    {
        public async Task<float[]> EmbedAsync(string text)
        {
            Settings settings = AIStorytellerMod.Settings;
            string validationError = ValidateConfig(settings);
            if (validationError != null)
                throw new InvalidOperationException(validationError);

            ProviderConfig config = settings.GetProviderConfig(settings.embeddingProviderId);
            LLmProviders provider = ProviderCatalog.ResolveProvider(config.providerId);

            TornadoApi api = BuildApi(config, provider);
            EmbeddingModel model = new EmbeddingModel(config.embeddingModel, provider);

            float[] raw = await api.Embeddings.GetEmbeddings(model, text ?? string.Empty);
            return Normalize(raw);
        }

        public float[] Embed(string text)
        {
            return EmbedAsync(text).GetAwaiter().GetResult();
        }

        public static string ValidateConfig(Settings settings)
        {
            if (settings == null)
                return "Mod settings are not available.";

            if (settings.embeddingSource != "Provider")
                return null;

            if (string.IsNullOrEmpty(settings.embeddingProviderId))
                return "No embedding provider selected.";

            ProviderConfig config = settings.GetProviderConfig(settings.embeddingProviderId);
            if (config == null)
                return "Embedding provider configuration not found.";

            if (string.IsNullOrEmpty(config.apiKey))
                return "API key required for embedding provider " + config.providerId + ".";

            if (string.IsNullOrEmpty(config.embeddingModel))
                return "Embedding model required for " + config.providerId + ".";

            return null;
        }

        private static TornadoApi BuildApi(ProviderConfig config, LLmProviders provider)
        {
            if (ProviderCatalog.IsSelfHosted(provider) && !string.IsNullOrEmpty(config.baseUrl))
            {
                string apiKey = string.IsNullOrEmpty(config.apiKey) ? null : config.apiKey;
                return new TornadoApi(new Uri(config.baseUrl), apiKey, provider);
            }

            return new TornadoApi(new System.Collections.Generic.List<ProviderAuthentication>
            {
                new ProviderAuthentication(provider, config.apiKey, null)
            });
        }

        private static float[] Normalize(float[] vector)
        {
            if (vector == null)
                return new float[0];

            float norm = (float)Math.Sqrt(vector.Sum(value => value * value));
            if (norm <= 1e-8f)
                return vector;

            var normalized = new float[vector.Length];
            for (int index = 0; index < vector.Length; index++)
                normalized[index] = vector[index] / norm;

            return normalized;
        }

        public void Dispose()
        {
        }
    }
}
