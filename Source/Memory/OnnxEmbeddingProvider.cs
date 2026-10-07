using Microsoft.ML.OnnxRuntime;
using Microsoft.ML.OnnxRuntime.Tensors;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace RimTelligence.Memory
{
    public class OnnxEmbeddingProvider : IEmbeddingProvider, IDisposable
    {
        private readonly InferenceSession session;
        private readonly BertTokenizer tokenizer;
        private const int EmbeddingDim = 384;

        public OnnxEmbeddingProvider()
        {
            if (!NativeDependencyLoader.EnsureNativeLoaded("onnxruntime.dll"))
                throw new InvalidOperationException(
                    "onnxruntime.dll is unavailable; ONNX embeddings cannot be used.");

            string modelPath = ModPaths.Resolve(Path.Combine("Source", "Memory", "Models", "model.onnx"));
            string vocabPath = ModPaths.Resolve(Path.Combine("Source", "Memory", "Models", "vocab.txt"));

            if (!File.Exists(modelPath)) throw new FileNotFoundException("ONNX model not found", modelPath);
            if (!File.Exists(vocabPath)) throw new FileNotFoundException("vocab.txt not found", vocabPath);

            var options = new SessionOptions();
            session = new InferenceSession(modelPath, options);
            tokenizer = new BertTokenizer(vocabPath);

            ModLogger.Message("ONNX embedding model loaded from: " + modelPath);
        }

        public float[] Embed(string text)
        {
            var (inputIds, attentionMask, tokenTypeIds) = tokenizer.Tokenize(text ?? string.Empty);
            int seqLen = inputIds.Length;

            var inputIdsTensor = new DenseTensor<long>(inputIds, new[] { 1, seqLen });
            var attentionMaskTensor = new DenseTensor<long>(attentionMask, new[] { 1, seqLen });
            var tokenTypeIdsTensor = new DenseTensor<long>(tokenTypeIds, new[] { 1, seqLen });

            var inputs = new[]
            {
                NamedOnnxValue.CreateFromTensor<long>("input_ids", inputIdsTensor),
                NamedOnnxValue.CreateFromTensor<long>("attention_mask", attentionMaskTensor),
                NamedOnnxValue.CreateFromTensor<long>("token_type_ids", tokenTypeIdsTensor),
            };

            using (var results = session.Run(inputs))
            {
                var lastHiddenState = results.First(result => result.Name == "last_hidden_state")
                    .AsTensor<float>();
                return MeanPoolAndNormalize(lastHiddenState, attentionMask, seqLen);
            }
        }

        public Task<float[]> EmbedAsync(string text)
        {
            return Task.Run(() => Embed(text));
        }

        private float[] MeanPoolAndNormalize(Microsoft.ML.OnnxRuntime.Tensors.Tensor<float> hidden, long[] attentionMask, int seqLen)
        {
            var pooled = new float[EmbeddingDim];
            float maskSum = 0f;

            for (int tokenIndex = 0; tokenIndex < seqLen; tokenIndex++)
            {
                if (attentionMask[tokenIndex] == 0) continue;
                maskSum += 1f;
                for (int dimIndex = 0; dimIndex < EmbeddingDim; dimIndex++)
                    pooled[dimIndex] += hidden[0, tokenIndex, dimIndex];
            }

            if (maskSum > 0)
                for (int dimIndex = 0; dimIndex < EmbeddingDim; dimIndex++)
                    pooled[dimIndex] /= maskSum;

            float norm = (float)Math.Sqrt(pooled.Sum(value => value * value));
            if (norm > 1e-8f)
                for (int dimIndex = 0; dimIndex < EmbeddingDim; dimIndex++)
                    pooled[dimIndex] /= norm;

            return pooled;
        }

        public void Dispose()
        {
            session?.Dispose();
        }
    }
}