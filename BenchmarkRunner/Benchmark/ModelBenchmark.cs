using RimTelligence.CustomEvents;
using RimTelligence.CustomEvents.Handlers;
using RimTelligence.CustomEvents.Prompting;
using RimTelligence.CustomEvents.Workers;
using RimTelligence.DataSnapshots;
using RimTelligence.Llm;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

namespace RimTelligence.Benchmark
{
    internal static class ModelBenchmark
    {
        public static async Task<BenchmarkResult> RunAsync(
            BenchmarkProvider provider,
            string model,
            string prompt,
            List<IEffect> chosenEvents, ColonySnapshot snapshot)
        {
            ProviderConfig providerConfig = BuildProviderConfig(provider, model);

            BenchmarkResult warmup = new BenchmarkResult
            {
                providerName = provider.name,
                providerId = providerConfig.providerId,
                endpoint = providerConfig.baseUrl,
                model = model
            };

            await WakeUpAsync(providerConfig, model, warmup);

            return await RunPromptAsync(providerConfig, provider.name, model, prompt, chosenEvents, snapshot, warmup.warmUpMs);
        }

        internal static ProviderConfig BuildProviderConfig(BenchmarkProvider provider, string model)
        {
            string providerId = string.IsNullOrWhiteSpace(provider.providerId)
                ? ProviderCatalog.CustomProviderId
                : provider.providerId;

            return new ProviderConfig
            {
                providerId = providerId,
                apiKey = provider.apiKey ?? string.Empty,
                chatModel = model,
                baseUrl = provider.endpoint ?? string.Empty
            };
        }

        internal static async Task<long> WarmUpAsync(ProviderConfig providerConfig, string model)
        {
            BenchmarkResult tmp = new BenchmarkResult();
            await WakeUpAsync(providerConfig, model, tmp);
            return tmp.warmUpMs;
        }

        internal static async Task<BenchmarkResult> RunPromptAsync(
            ProviderConfig providerConfig,
            string providerName,
            string model,
            string prompt,
            List<IEffect> chosenEvents,
            ColonySnapshot snapshot,
            long warmUpMs = 0)
        {
            BenchmarkResult result = new BenchmarkResult
            {
                providerName = providerName,
                providerId = providerConfig.providerId,
                endpoint = providerConfig.baseUrl,
                model = model,
                warmUpMs = warmUpMs
            };

            if (chosenEvents != null && chosenEvents.Count == 1 && chosenEvents[0]?.Config != null)
            {
                result.cueEffect = chosenEvents[0].Config.EffectName;
                result.cueEffects = new List<string> { result.cueEffect };
                result.expectedEffectCount = 1;
            }
            else if (chosenEvents != null && chosenEvents.Count > 1)
            {
                result.cueEffects = chosenEvents.Where(e => e?.Config != null).Select(e => e.Config.EffectName).ToList();
                result.cueEffect = string.Join(" + ", result.cueEffects);
                result.expectedEffectCount = result.cueEffects.Count;
            }

            await MeasureAsync(providerConfig, prompt, chosenEvents,snapshot,result);
            return result;
        }

        private static async Task WakeUpAsync(ProviderConfig providerConfig, string model, BenchmarkResult result)
        {
            ModLogger.Message(
                $"[Benchmark] Hello! Waking up '{model}' at {providerConfig.baseUrl} — " +
                "a local model can take a while to load...");

            Stopwatch stopwatch = Stopwatch.StartNew();
            LlmConnectionResult response = null;
            response = await new LLMCommunicationHandler().WarmUpAsync("hello", providerConfig);

            stopwatch.Stop();

            result.warmUpMs = stopwatch.ElapsedMilliseconds;
        }

        private static async Task MeasureAsync(
            ProviderConfig providerConfig,
            string prompt,
            List<IEffect> chosenEvents,
            ColonySnapshot snapshot,
            BenchmarkResult result)
        {
            LlmConnectionResult lastResponse = null;
            Stopwatch stopwatch = Stopwatch.StartNew();

            try
            {
                // Single attempt and no failure letter: a benchmark wants the raw result, not retries.
                LlmCustomEvent parsedEvent = await IncidentWorker_ProceduralEvent.RequestEventAsync(
                    prompt,
                    chosenEvents,
                    providerConfig,
                    maxAttempts: 1,
                    announceFailure: false,
                    onResponse: response => lastResponse = response,
                    snapshot: snapshot);

                stopwatch.Stop();
                result.elapsedMs = stopwatch.ElapsedMilliseconds;

                if (parsedEvent != null)
                {
                    result.success = true;
                    result.letterTitle = parsedEvent.letterTitle;
                    result.letterText = parsedEvent.letterText;
                    result.effects = parsedEvent.effects?
                        .Select(effect => effect.EffectName)
                        .ToList();
                    result.returnedEffectCount = result.effects?.Count ?? 0;

                    if (result.cueEffects != null && result.cueEffects.Count > 0)
                    {
                        var returnedSet = new HashSet<string>(result.effects ?? Enumerable.Empty<string>(), StringComparer.OrdinalIgnoreCase);
                        result.allCuedEffectsReturned = result.cueEffects.All(c => returnedSet.Contains(c));
                    }
                    else
                    {
                        result.allCuedEffectsReturned = true;
                    }
                }
            }
            catch (Exception exception)
            {
                stopwatch.Stop();
                result.elapsedMs = stopwatch.ElapsedMilliseconds;
                result.error = exception.Message;
            }

            if (lastResponse != null)
            {
                result.promptTokens = lastResponse.promptTokens;
                result.completionTokens = lastResponse.completionTokens;
                result.reasoningTokens = lastResponse.reasoningTokens;
                result.rawResponse = lastResponse.responseText;

                if (!lastResponse.isSuccess)
                    result.error = lastResponse.errorMessage;
            }

            // RequestEventAsync hides the parse reason on failure, so re-derive it from the raw text.
            if (!result.success
                && lastResponse != null
                && lastResponse.isSuccess
                && !string.IsNullOrEmpty(lastResponse.responseText))
            {
                result.error = DescribeParseFailure(lastResponse.responseText, chosenEvents, snapshot)
                    ?? result.error
                    ?? "No usable event produced.";
            }

            if (!result.success && string.IsNullOrEmpty(result.error))
                result.error = "No usable event produced.";
        }

        private static string DescribeParseFailure(string rawResponse, List<IEffect> chosenEvents, ColonySnapshot snapshot)
        {
            try
            {
                LlmParseResult<LlmCustomEvent> reparse =
                    LlmResponseParser.ParseEvent(rawResponse, chosenEvents, snapshot);
                return reparse.errorMessage;
            }
            catch (Exception exception)
            {
                return "Parser threw: " + exception.Message;
            }
        }
    }
}
