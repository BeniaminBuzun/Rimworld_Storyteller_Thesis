using LlmTornado;
using LlmTornado.Chat;
using LlmTornado.Chat.Models;
using LlmTornado.Code;
using RimTelligence.AIStoryteller;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace RimTelligence.Llm
{
    public class LLMCommunicationHandler
    {
        private enum Step { Structured, Json, Text }
        private enum Tri { Unknown, Yes, No }

        private sealed class Capabilities
        {
            public volatile Tri Structured = Tri.Unknown;
            public volatile Tri Json = Tri.Unknown;
            public volatile Tri Reasoning = Tri.Unknown;
        }

        private sealed class StepOutcome
        {
            public ChatRichResponse Response;
            public LlmErrorInfo Error;
        }

        // Shared across handler instances so what we learn about a model survives new LLMCommunicationHandler() calls.
        private static readonly ConcurrentDictionary<string, Capabilities> CapabilityCache =
            new ConcurrentDictionary<string, Capabilities>();
        private static readonly ConcurrentDictionary<string, TornadoApi> ApiCache =
            new ConcurrentDictionary<string, TornadoApi>();

        /// <summary>Retries of the SAME request for transient errors. 0 disables.</summary>
        public int MaxTransientRetries = 2;

        /// <summary>Per-request timeout. The underlying HTTP call may keep running after this.</summary>
        public TimeSpan RequestTimeout = TimeSpan.FromSeconds(360);

        /// <summary>
        /// Off by default: sending ReasoningEffort to a non-reasoning model is a 400.
        /// When on, a "reasoning" rejection just drops the parameter and retries the same mode.
        /// </summary>
        private bool PreferLowReasoning = false;

        // ------------------------------------------------------------------ public API

        public Task<LlmConnectionResult> GetLlmResponseWithDetailsAsync(
            string prompt, object jsonSchema = null, string schemaName = "rimtelligence_event",
            ProviderConfig configOverride = null, LlmOutputMode mode = LlmOutputMode.Auto)
        {
            return RunAsync(prompt, jsonSchema, schemaName, configOverride, mode);
        }

        public Task<LlmConnectionResult> WarmUpAsync(string greeting = "hello", ProviderConfig configOverride = null)
        {
            return RunAsync(greeting, null, null, configOverride, LlmOutputMode.Text);
        }

        // ------------------------------------------------------------------ core

        private async Task<LlmConnectionResult> RunAsync(
            string prompt, object jsonSchema, string schemaName, ProviderConfig configOverride, LlmOutputMode mode)
        {
            ProviderConfig config = configOverride ?? AIStorytellerMod.Settings?.ActiveProvider;
            if (config == null)
                return LlmConnectionResult.Failure("unknown", "unknown",
                    "No provider configured. Select and apply a provider in the mod settings.", LlmErrorKind.Config);

            string validationError = ValidateChatConfig(config);
            if (validationError != null)
                return LlmConnectionResult.Failure(config.providerId, config.chatModel, validationError, LlmErrorKind.Config);

            try
            {
                LLmProviders provider = ProviderCatalog.ResolveProvider(config.providerId);
                TornadoApi api = GetApi(config, provider);
                ChatModel model = ProviderCatalog.BuildChatModel(config);
                Capabilities caps = CapabilityCache.GetOrAdd(CapabilityKey(config), _ => new Capabilities());

                LlmErrorInfo lastError = LlmErrorClassifier.Make(LlmErrorKind.Unknown, "LLM error.");

                foreach (Step step in BuildLadder(mode, jsonSchema, caps))
                {
                    StepOutcome outcome = await RunStepAsync(api, model, caps, step, prompt, jsonSchema, schemaName)
                        .ConfigureAwait(false);

                    if (outcome.Error == null)
                    {
                        ChatRichResponse rich = outcome.Response;
                        string text = rich?.Text;

                        if (string.IsNullOrWhiteSpace(text))
                        {
                            // Often a reasoning model that spent its whole budget thinking. Try a simpler mode.
                            lastError = LlmErrorClassifier.Make(LlmErrorKind.Unknown, "Empty response from provider.");
                            ModLogger.Warning("[LLM] Empty response in " + StepName(step) + " mode; trying the next mode.");
                            continue;
                        }

                        MarkSupported(caps, step);
                        LogResponse(config, rich, text, StepName(step));
                        return LlmConnectionResult.Success(
                            config.providerId, config.chatModel, text,
                            rich.Usage?.PromptTokens ?? 0,
                            rich.Usage?.CompletionTokens ?? 0,
                            rich.Usage?.CompletionTokensDetails?.ReasoningTokens ?? 0,
                            StepName(step));
                    }

                    lastError = outcome.Error;

                    bool featureProblem = lastError.Kind == LlmErrorKind.Unsupported
                                          || lastError.Kind == LlmErrorKind.BadRequest;
                    if (featureProblem && step != Step.Text)
                    {
                        // Only a keyword-confirmed rejection is remembered. A plain 400 might be OUR bug
                        // (for example an invalid schema), and must not disable the mode for the whole session.
                        if (lastError.Kind == LlmErrorKind.Unsupported)
                            MarkUnsupported(caps, step);

                        ModLogger.Warning("[LLM] " + StepName(step) + " mode rejected (" + lastError.Kind
                                          + "): " + lastError.Raw + " Falling back.");
                        continue;
                    }

                    // Auth / quota / 404 / config, or transient errors that already used up their retries,
                    // or a failure in plain text: a weaker mode will not help.
                    break;
                }

                return LlmConnectionResult.Failure(config.providerId, config.chatModel, lastError.Message, lastError.Kind);
            }
            catch (Exception exception)
            {
                LlmErrorInfo info = LlmErrorClassifier.Classify(exception);
                return LlmConnectionResult.Failure(config.providerId, config.chatModel, info.Message, info.Kind);
            }
        }

        /// <summary>One mode: sends the request, retrying only transient failures.</summary>
        private async Task<StepOutcome> RunStepAsync(
            TornadoApi api, ChatModel model, Capabilities caps, Step step,
            string prompt, object jsonSchema, string schemaName)
        {
            bool reasoning = PreferLowReasoning && caps.Reasoning != Tri.No && step != Step.Text;
            int transientRetries = 0;

            while (true)
            {
                try
                {
                    ChatRichResponse rich = await SendOnce(api, model, step, reasoning, prompt, jsonSchema, schemaName)
                        .ConfigureAwait(false);
                    if (reasoning) caps.Reasoning = Tri.Yes;
                    return new StepOutcome { Response = rich };
                }
                catch (Exception exception)
                {
                    LlmErrorInfo error = LlmErrorClassifier.Classify(exception);

                    bool featureProblem = error.Kind == LlmErrorKind.Unsupported || error.Kind == LlmErrorKind.BadRequest;
                    if (reasoning && featureProblem && error.Raw.Contains("reasoning"))
                    {
                        // Not a mode problem: just this parameter. Drop it and repeat the same mode.
                        caps.Reasoning = Tri.No;
                        reasoning = false;
                        ModLogger.Message("[LLM] Model rejects reasoning effort; continuing without it.");
                        continue;
                    }

                    if (error.IsRetryable && transientRetries < MaxTransientRetries)
                    {
                        transientRetries++;
                        TimeSpan delay = error.RetryAfter ?? Backoff(transientRetries);
                        ModLogger.Warning("[LLM] " + error.Kind + " in " + StepName(step) + " mode; retry "
                                          + transientRetries + "/" + MaxTransientRetries + " in "
                                          + delay.TotalSeconds.ToString("F1") + "s.");
                        await Task.Delay(delay).ConfigureAwait(false);
                        continue;
                    }

                    return new StepOutcome { Error = error };
                }
            }
        }

        private async Task<ChatRichResponse> SendOnce(
            TornadoApi api, ChatModel model, Step step, bool reasoning,
            string prompt, object jsonSchema, string schemaName)
        {
            ChatRequest request = new ChatRequest { Model = model };
            if (reasoning)
                request.ReasoningEffort = ChatReasoningEfforts.Low;

            if (step == Step.Structured)
                request.ResponseFormat = ChatRequestResponseFormats.StructuredJson(schemaName, jsonSchema);
            else if (step == Step.Json)
                request.ResponseFormat = ChatRequestResponseFormats.Json;

            Conversation chat = api.Chat.CreateConversation(request);
            chat.AppendUserInput(prompt);
            return await WithTimeout(chat.GetResponseRich(), RequestTimeout).ConfigureAwait(false);
        }

        // ------------------------------------------------------------------ mode ladder

        private static List<Step> BuildLadder(LlmOutputMode mode, object jsonSchema, Capabilities caps)
        {
            if (mode == LlmOutputMode.Auto)
                mode = jsonSchema != null ? LlmOutputMode.Schema : LlmOutputMode.Text;

            // A schema mode with no schema degrades to plain JSON instead of sending garbage.
            if (mode == LlmOutputMode.Schema && jsonSchema == null)
                mode = LlmOutputMode.Json;

            var ladder = new List<Step>(3);

            if (mode == LlmOutputMode.Schema && caps.Structured != Tri.No)
                ladder.Add(Step.Structured);

            if ((mode == LlmOutputMode.Schema || mode == LlmOutputMode.Json) && caps.Json != Tri.No)
                ladder.Add(Step.Json);

            ladder.Add(Step.Text);   // always last, never skipped
            return ladder;
        }

        private static void MarkSupported(Capabilities caps, Step step)
        {
            if (step == Step.Structured) caps.Structured = Tri.Yes;
            else if (step == Step.Json) caps.Json = Tri.Yes;
        }

        private static void MarkUnsupported(Capabilities caps, Step step)
        {
            if (step == Step.Structured) caps.Structured = Tri.No;
            else if (step == Step.Json)
            {
                caps.Json = Tri.No;
                // A model without JSON mode almost certainly has no schema mode either.
                if (caps.Structured == Tri.Unknown) caps.Structured = Tri.No;
            }
        }

        // ------------------------------------------------------------------ helpers

        private static string CapabilityKey(ProviderConfig config)
        {
            return config.providerId + "|" + config.chatModel + "|" + config.baseUrl;
        }

        private static string StepName(Step step)
        {
            switch (step)
            {
                case Step.Structured: return "structured";
                case Step.Json: return "json-mode";
                default: return "plain-text";
            }
        }

        private static TimeSpan Backoff(int retryNumber)
        {
            double seconds = Math.Min(30.0, Math.Pow(2.0, retryNumber));
            double jitter = new Random(Guid.NewGuid().GetHashCode()).NextDouble();
            return TimeSpan.FromSeconds(seconds + jitter);
        }

        private static async Task<T> WithTimeout<T>(Task<T> task, TimeSpan timeout)
        {
            using (var delayCts = new CancellationTokenSource())
            {
                Task finished = await Task.WhenAny(task, Task.Delay(timeout, delayCts.Token)).ConfigureAwait(false);
                if (finished != task)
                {
                    // Observe the abandoned task so a late failure doesn't surface as an unobserved exception.
                    task.ContinueWith(t => { var ignored = t.Exception; }, TaskContinuationOptions.OnlyOnFaulted);
                    throw new TimeoutException("LLM request timed out after " + timeout.TotalSeconds + "s.");
                }

                delayCts.Cancel();
                return await task.ConfigureAwait(false);
            }
        }

        private static TornadoApi GetApi(ProviderConfig config, LLmProviders provider)
        {
            string key = config.providerId + "|" + config.apiKey + "|" + config.baseUrl;
            return ApiCache.GetOrAdd(key, _ => BuildApi(config, provider));
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

        private static void LogResponse(ProviderConfig config, ChatRichResponse rich, string response, string modeUsed)
        {
            int promptTokens = rich.Usage?.PromptTokens ?? 0;
            int completionTokens = rich.Usage?.CompletionTokens ?? 0;
            int reasoningTokens = rich.Usage?.CompletionTokensDetails?.ReasoningTokens ?? 0;
            string finish = rich.FinishReason.ToString();

            ModLogger.Message(
                "Full response: " + response + "  Metadata (" + config.providerId + "/" + config.chatModel
                + ", mode=" + modeUsed + "): " + response.Length + " chars, Finish=" + finish
                + ", tokens Prompt=" + promptTokens + " Completion=" + completionTokens + " Reasoning=" + reasoningTokens);

            if (finish.IndexOf("length", StringComparison.OrdinalIgnoreCase) >= 0)
                ModLogger.Warning("[LLM] Response was cut off by the token limit; JSON parsing will likely fail.");
        }
    }
}