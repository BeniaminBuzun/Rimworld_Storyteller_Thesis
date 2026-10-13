using System;
using System.Collections.Generic;
using System.Threading.Tasks;

using LlmTornado;
using LlmTornado.Chat;
using LlmTornado.Code;
using RimTelligence.AIStoryteller;

namespace RimTelligence.Llm
{
    public class LLMCommunicationHandler
    {
        TornadoApi api = new TornadoApi("your-api-key");

        public async void StartNewConversation()
        {
            
        }



        public async Task<LlmConnectionResult> GetLlmResponseWithDetailsAsync(
            string prompt, object jsonSchema = null, string schemaName = "rimtelligence_event")
        {
            ProviderConfig config = AIStorytellerMod.Settings?.ActiveProvider;
            if (config == null)
                return LlmConnectionResult.Failure("unknown", "unknown",
                    "No provider configured. Select and apply a provider in the mod settings.");

            string validationError = ValidateChatConfig(config);
            if (validationError != null)
                return LlmConnectionResult.Failure(config.providerId, config.chatModel, validationError);

            return await RunChatRequestAsync(config, prompt, jsonSchema, schemaName);
        }

        private async Task<LlmConnectionResult> RunChatRequestAsync(
            ProviderConfig config, string prompt, object jsonSchema, string schemaName)
        {
            if (config == null)
                return LlmConnectionResult.Failure("unknown", "unknown", "No provider configured.");

            LLmProviders provider = ProviderCatalog.ResolveProvider(config.providerId);

            try
            {
                TornadoApi api = BuildApi(config, provider);

                (ChatRichResponse response, string modeUsed) result =
                    await TryModes(api, config, prompt, jsonSchema, schemaName);

                ChatRichResponse richResponse = result.response;
                string response = richResponse.Text;
                LogResponse(config, richResponse, response, result.modeUsed);

                if (string.IsNullOrWhiteSpace(response))
                    return LlmConnectionResult.Failure(config.providerId, config.chatModel, "Empty response from provider.");

                return LlmConnectionResult.Success(
                    config.providerId, config.chatModel, response,
                    richResponse.Usage?.PromptTokens ?? 0,
                    richResponse.Usage?.CompletionTokens ?? 0,
                    richResponse.Usage?.CompletionTokensDetails?.ReasoningTokens ?? 0);
            }
            catch (Exception exception)
            {
                return LlmConnectionResult.Failure(config.providerId, config.chatModel, DescribeError(exception));
            }
        }

        private async Task<(ChatRichResponse response, string modeUsed)> TryModes(
            TornadoApi api, ProviderConfig config, string prompt, object jsonSchema, string schemaName)
        {
            // full structured json schema enforced on model level (if supported)
            if (jsonSchema != null)
            {
                try
                {
                    ChatRequest request = BuildRequest(config, reasoningLow: true);
                    request.ResponseFormat = ChatRequestResponseFormats.StructuredJson(schemaName, jsonSchema);
                    return (await Send(api, request, prompt), "structured+reasoning-low");
                }
                catch (Exception ex)
                {
                    ModLogger.Warning($"[LLM] Structured schema request failed ({ex.Message}); falling back to plain JSON mode.");
                }
            }

            // plain JSON fallback
            try
            {
                ChatRequest request = BuildRequest(config, reasoningLow: true);
                request.ResponseFormat = ChatRequestResponseFormats.Json;
                return (await Send(api, request, prompt), "json-mode+reasoning-low");
            }
            catch (Exception ex)
            {
                ModLogger.Warning($"[LLM] JSON mode request failed ({ex.Message}); falling back to plain text mode.");
            }

            // plain text fallback
            ChatRequest fallbackRequest = BuildRequest(config, reasoningLow: false);
            return (await Send(api, fallbackRequest, prompt), "plain-text-fallback");
        }

        private static ChatRequest BuildRequest(ProviderConfig config, bool reasoningLow)
        {
            ChatRequest request = new ChatRequest { Model = ProviderCatalog.BuildChatModel(config) };
            if (reasoningLow)
                request.ReasoningEffort = ChatReasoningEfforts.Low;
            return request;
        }

        private static async Task<ChatRichResponse> Send(TornadoApi api, ChatRequest request, string prompt)
        {
            Conversation chat =     Chat.CreateConversation(request);
            chat.AppendUserInput(prompt);
            return await chat.GetResponseRich();
        }

        private static TornadoApi BuildApi(ProviderConfig config, LLmProviders provider)
        {
            if (ProviderCatalog.IsSelfHosted(provider) && !string.IsNullOrEmpty(config.baseUrl))
            {
                string apiKey = string.IsNullOrEmpty(config.apiKey) ? null : config.apiKey;
                return new TornadoApi(new Uri(config.baseUrl), apiKey, provider);
            }

            return new TornadoApi(new List<ProviderAuthentication>
            {
                new ProviderAuthentication(provider, config.apiKey, null)
            });
        }

        private static string DescribeError(Exception exception)
        {
            if (exception == null) return "Unknown error.";
            string message = exception.Message;
            if (string.IsNullOrEmpty(message)) return "Unknown error.";
            if (message.Contains("429") || message.IndexOf("rate_limit", StringComparison.OrdinalIgnoreCase) >= 0 || message.IndexOf("toomanyrequests", StringComparison.OrdinalIgnoreCase) >= 0)
                return "Rate limit exceeded (HTTP 429).";
            if (message.IndexOf("401", StringComparison.OrdinalIgnoreCase) >= 0 || message.IndexOf("unauthorized", StringComparison.OrdinalIgnoreCase) >= 0 || message.IndexOf("authentication", StringComparison.OrdinalIgnoreCase) >= 0)
                return "Authentication failed. Check the API key.";
            if (message.IndexOf("404", StringComparison.OrdinalIgnoreCase) >= 0 || message.IndexOf("not found", StringComparison.OrdinalIgnoreCase) >= 0)
                return "Provider endpoint or model not found. Check the model name and server URL.";
            if (message.IndexOf("uri", StringComparison.OrdinalIgnoreCase) >= 0)
                return "Invalid server URL.";
            if (message.IndexOf("connection", StringComparison.OrdinalIgnoreCase) >= 0)
                return "Connection failed. Check the server URL or your network.";
            return "LLM error";
        }

        private static string ValidateChatConfig(ProviderConfig config)
        {
            if (config == null) return "No provider selected. Configure a provider in the mod settings.";
            if (string.IsNullOrEmpty(config.chatModel)) return "Model required for " + config.providerId + ".";
            LLmProviders provider = ProviderCatalog.ResolveProvider(config.providerId);
            if (provider == LLmProviders.Unknown) return "Unknown provider '" + config.providerId + "'.";
            if (ProviderCatalog.IsSelfHosted(provider))
            {
                if (string.IsNullOrEmpty(config.baseUrl))
                    return "Server URL required for self-hosted provider " + config.providerId + ".";
            }
            else if (string.IsNullOrEmpty(config.apiKey))
            {
                return "API key required for " + config.providerId + ".";
            }
            return null;
        }

        private void LogResponse(ProviderConfig config, ChatRichResponse rich, string response, string modeUsed)
        {
            int promptTokens = rich.Usage?.PromptTokens ?? 0;
            int completionTokens = rich.Usage?.CompletionTokens ?? 0;
            int reasoningTokens = rich.Usage?.CompletionTokensDetails?.ReasoningTokens ?? 0;
            ModLogger.Message(
                "Full response: " + response + "  Metadata (" + config.providerId + "/" + config.chatModel + ", mode=" + modeUsed + "): " +
                response.Length + " chars, Finish=" + rich.FinishReason +
                ", tokens Prompt=" + promptTokens + " Completion=" + completionTokens + " Reasoning=" + reasoningTokens);
        }
    }
}