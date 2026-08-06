using RimTelligence.Memory;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace RimTelligence.CustomEvents.Prompting
{
    public class RagContextProvider
    {
        private readonly IEmbeddingProvider _embedding;
        private int _topK = 5;
        private float _threshold = 0.5f;
        private const int TokenBudgetChars = 2000;

        public RagContextProvider(IEmbeddingProvider embedding)
        {
            _embedding = embedding ?? throw new ArgumentNullException(nameof(embedding));
        }

        public void Configure(int topK, float threshold)
        {
            _topK = topK > 0 ? topK : 5;
            _threshold = threshold;
        }

        public async Task<RagQueryResult> QueryAsync(string narrative)
        {
            if (string.IsNullOrWhiteSpace(narrative))
                return new RagQueryResult();

            float[] queryEmbedding = await Task.Run(() => _embedding.Embed(narrative));

            var staticResults = new System.Collections.Generic.List<RagResult>();
            var dynamicResults = new System.Collections.Generic.List<RagResult>();

            try
            {
                var staticStore = new StaticRagStore(RagBootstrap.DbPath);
                staticResults = staticStore.Query(queryEmbedding, topK: _topK);
            }
            catch (Exception exception)
            {
                ModLogger.Warning($"[RAG] Static store query failed, continuing with dynamic only: {exception.Message}");
            }

            try
            {
                var dynamicStore = new DynamicRagStore(RagBootstrap.DbPath);
                dynamicResults = dynamicStore.Query(queryEmbedding, topK: _topK);
            }
            catch (Exception exception)
            {
                ModLogger.Warning($"[RAG] Dynamic store query failed, continuing with static only: {exception.Message}");
            }

            var result = new RagQueryResult
            {
                StaticResults = staticResults.Where(ragResult => ragResult.Score >= _threshold).ToList(),
                DynamicResults = dynamicResults.Where(ragResult => ragResult.Score >= _threshold).ToList()
            };

            CheckTokenBudget(result);
            return result;
        }

        private void CheckTokenBudget(RagQueryResult result)
        {
            int totalChars = 0;
            foreach (var ragResult in result.StaticResults) totalChars += ragResult.Text?.Length ?? 0;
            foreach (var ragResult in result.DynamicResults) totalChars += ragResult.Text?.Length ?? 0;

            if (totalChars > TokenBudgetChars)
                ModLogger.Warning($"[RAG] Token budget exceeded: ~{totalChars / 4} estimated tokens ({totalChars} chars, limit {TokenBudgetChars / 4})");
        }
    }
}
