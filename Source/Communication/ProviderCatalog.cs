using LlmTornado.Chat.Models;
using LlmTornado.Code;
using LlmTornado.Code.Models;
using LlmTornado.Embedding.Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace RimTelligence.Llm
{
    public static class ProviderCatalog
    {
        public static readonly string CustomProviderId = LLmProviders.Custom.ToString();

        public static List<LLmProviders> ChatCapableProviders { get; private set; }
        public static List<LLmProviders> EmbeddingsOnlyProviders { get; private set; }
        public static List<LLmProviders> EmbeddingProviders { get; private set; }

        static ProviderCatalog()
        {
            ChatCapableProviders = ChatModel.AllModels
                .Select(model => model.Provider)
                .Distinct()
                .Where(provider => provider != LLmProviders.Unknown && provider != LLmProviders.Length)
                .ToList();

            if (!ChatCapableProviders.Contains(LLmProviders.Custom))
                ChatCapableProviders.Add(LLmProviders.Custom);

            EmbeddingProviders = EmbeddingModel.AllModels
                .Select(model => model.Provider)
                .Distinct()
                .Where(provider => provider != LLmProviders.Unknown && provider != LLmProviders.Length)
                .ToList();

            if (!EmbeddingProviders.Contains(LLmProviders.Custom))
                EmbeddingProviders.Add(LLmProviders.Custom);

            EmbeddingsOnlyProviders = EmbeddingProviders
                .Where(provider => !ChatCapableProviders.Contains(provider))
                .ToList();
        }

        public static bool IsSelfHosted(LLmProviders provider)
        {
            return provider == LLmProviders.Custom;
        }

        public static LLmProviders ResolveProvider(string providerId)
        {
            if (string.IsNullOrEmpty(providerId))
                return LLmProviders.Unknown;

            if (Enum.TryParse(providerId, true, out LLmProviders parsed))
                return parsed;

            return LLmProviders.Unknown;
        }

        public static List<string> GetChatModelNames(LLmProviders provider)
        {
            if (provider == LLmProviders.Custom)
                return new List<string>();

            return ChatModel.AllModels
                .Where(model => model.Provider == provider)
                .Select(model => model.Name)
                .OrderBy(name => name, StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        public static List<string> GetEmbeddingModelNames(LLmProviders provider)
        {
            return EmbeddingModel.AllModels
                .Where(model => model.Provider == provider)
                .Select(model => model.Name)
                .OrderBy(name => name, StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        public static ChatModel BuildChatModel(ProviderConfig config)
        {
            LLmProviders provider = ResolveProvider(config.providerId);
            string modelName = string.IsNullOrEmpty(config.chatModel) ? "" : config.chatModel;

            if (!string.IsNullOrEmpty(modelName)
                && ChatModel.AllModelsMap != null
                && ChatModel.AllModelsMap.TryGetValue(modelName, out IModel catalogMatch)
                && catalogMatch is ChatModel catalogChatModel
                && catalogChatModel.Provider == provider)
            {
                return catalogChatModel;
            }

            return new ChatModel(modelName, provider);
        }
    }
}
