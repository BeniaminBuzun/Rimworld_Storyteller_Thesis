using RimTelligence.CustomEvents.EventSelector;
using RimTelligence.CustomEvents.Execution;
using RimTelligence.CustomEvents.Handlers;
using RimTelligence.CustomEvents.Prompting;
using RimTelligence.DataSnapshots;
using RimTelligence.Llm;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Verse;

namespace RimTelligence.CustomEvents.Workers
{
    public class IncidentWorker_ProceduralEvent : IncidentWorker
    {
        private const int MaxAttempts = 3;
        private static readonly TimeSpan RetryDelay = TimeSpan.FromSeconds(60);

        protected override bool TryExecuteWorker(IncidentParms parms)
        {
            if (!(parms.target is Map map))
            {
                ModLogger.Error("TryExecuteWorker: parms.target is not a Map.");
                return false;
            }

            ModLogger.Message("ProceduralEvent triggered, capturing snapshot...");

            // Everything touching game state stays on the main thread.
            ColonySnapshot snapshot = ColonySnapshotCapturer.Capture(map);
            DataExporter.ExportColonySnapshot(snapshot);
            string prompt = BuildPrompt(snapshot, out List<IEffect> chosenEvents);

            // Fire and forget; the result is marshalled back via ExecutionQueue.
            _ = RunEventAsync(prompt, chosenEvents, map);
            return true;
        }

        private async Task RunEventAsync(string prompt, List<IEffect> chosenEvents, Map map)
        {
            try
            {
                LlmCustomEvent finalEvent = await RequestEventAsync(prompt, chosenEvents);
                if (finalEvent != null)
                {
                    FireLlmEvent(finalEvent, map);
                }
            }
            catch (Exception exception)
            {
                ModLogger.Error("RunEventAsync failed: " + exception);
            }
        }

        private void FireLlmEvent(LlmCustomEvent finalEvent, Map map)
        {
            // Game thread execution
            RimTelligenceDispatcher.ExecutionQueue.Enqueue(() =>
            {
                List<EffectOutcome> outcomes = LlmEffectExecutor.ExecuteAll(finalEvent, map);

                string summary = LlmEffectExecutor.BuildSummary(finalEvent, outcomes);
                string fullText = $"{finalEvent.letterText ?? "Something happened."}\n\n{summary}";

                var validTargets = outcomes.Where(o => o.Target.IsValid).Select(o => o.Target).ToList();

                var letter = new ChoiceLetter_Procedural
                {
                    def = LetterDefOf.ThreatSmall,
                    ID = Find.UniqueIDsManager.GetNextLetterID(),
                    Label = finalEvent.letterTitle ?? "Strange Event",
                    Text = fullText,
                    lookTargets = validTargets.Count > 0
                        ? new LookTargets(validTargets)
                        : null,
                    jumpLabels = outcomes.Select(o => o.Label).ToList(),
                    jumpTargets = outcomes.Select(o => o.Target).ToList()
                };

                Find.LetterStack.ReceiveLetter(letter);
            });
        }

        private string BuildPrompt(ColonySnapshot snapshot, out List<IEffect> chosenEvents)
        {
            chosenEvents = HeuristicEventSelector.Select(snapshot);
            string fullNarrative = SnapshotNarrator.Narrate(snapshot);

            return ProperPromptGenerator.BuildSystemPrompt(
                fullNarrative,
                chosenEvents,
                snapshot);
        }

        public static async Task<LlmCustomEvent> RequestEventAsync(
           string prompt,
           List<IEffect> chosenEvents,
           ProviderConfig providerConfig = null,
           int maxAttempts = MaxAttempts,
           bool announceFailure = true,
           Action<LlmConnectionResult> onResponse = null)
        {
            LLMCommunicationHandler communication = new LLMCommunicationHandler();

            string llmProvider = "unknown";
            string llmModel = "unknown";
            string lastFailureReason = "Unknown error.";

            for (int attempt = 1; attempt <= maxAttempts; attempt++)
            {
                if (attempt > 1)
                {
                    await Task.Delay(RetryDelay);
                }

                string responseRaw;
                int promptTokens, completionTokens, reasoningTokens;

                try
                {
                    LlmConnectionResult result = await communication.GetLlmResponseWithDetailsAsync(
                        prompt, configOverride: providerConfig);
                    onResponse?.Invoke(result);

                    llmProvider = result?.providerId ?? "unknown";
                    llmModel = result?.model ?? "unknown";

                    if (result == null || !result.isSuccess)
                    {
                        lastFailureReason = result?.errorMessage ?? "Unknown error";
                        ModLogger.Error($"[Step 2] Attempt {attempt}/{maxAttempts}: LLM call failed. Provider: {llmProvider}, Model: {llmModel}, Reason: {lastFailureReason}");
                        continue;
                    }

                    responseRaw = result.responseText;
                    promptTokens = result.promptTokens;
                    completionTokens = result.completionTokens;
                    reasoningTokens = result.reasoningTokens;
                }
                catch (Exception exception)
                {
                    lastFailureReason = exception.Message;
                    ModLogger.Error($"[Step 2] Attempt {attempt}/{maxAttempts}: LLM call threw: {lastFailureReason}");
                    continue;
                }

                if (string.IsNullOrWhiteSpace(responseRaw))
                {
                    lastFailureReason = "The language model returned an empty response.";
                    ModLogger.Error($"[Step 2] Attempt {attempt}/{maxAttempts}: {lastFailureReason}");
                    continue;
                }

                ModLogger.Message($"[Step 2] Attempt {attempt}/{maxAttempts}: LLM response ({llmProvider}/{llmModel}, {responseRaw.Length} chars, P={promptTokens} C={completionTokens} R={reasoningTokens}):\n{responseRaw}");

                LlmParseResult<LlmCustomEvent> parseResult;
                try
                {
                    parseResult = LlmResponseParser.ParseEvent(responseRaw, chosenEvents);
                }
                catch (Exception exception)
                {
                    lastFailureReason = "Parser threw: " + exception.Message;
                    ModLogger.Error($"[Step 2] Attempt {attempt}/{maxAttempts}: {lastFailureReason}");
                    continue;
                }

                if (!parseResult.isSuccess || parseResult.data == null)
                {
                    lastFailureReason = parseResult.errorMessage;
                    ModLogger.Error($"[Step 2] Attempt {attempt}/{maxAttempts}: Parse failed. Provider: {llmProvider}, Model: {llmModel}, Reason: {lastFailureReason}\nPreview: {parseResult.responsePreview}");
                    continue;
                }

                return parseResult.data; // success
            }

            // All attempts failed
            ModLogger.Error($"[Step 2] All {maxAttempts} attempts failed. Provider: {llmProvider}, Model: {llmModel}, Last reason: {lastFailureReason}");

            if (announceFailure)
            {
                RimTelligenceDispatcher.ExecutionQueue.Enqueue(() =>
                {
                    Find.LetterStack.ReceiveLetter(
                        "Storyteller LLM Unavailable",
                        "The storyteller's language model could not produce a usable response after " + maxAttempts + " attempts.\nProvider: " + llmProvider + "\nModel: " + llmModel + "\nLast error: " + lastFailureReason,
                        LetterDefOf.NegativeEvent);
                });
            }

            return null;
        }
    }
}