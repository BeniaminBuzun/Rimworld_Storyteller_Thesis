using RimTelligence.CustomEvents.EventSelector;
using RimTelligence.CustomEvents.Execution;
using RimTelligence.CustomEvents.Handlers;
using RimTelligence.CustomEvents.Prompting;
using RimTelligence.DataSnapshots;
using RimTelligence.Llm;
using RimTelligence.Memory;
using RimWorld;
using RimWorld.Planet;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Verse;

namespace RimTelligence.CustomEvents.Workers
{
    public class IncidentWorker_ProceduralEvent : IncidentWorker
    {
        protected override bool TryExecuteWorker(IncidentParms parms)
        {
            if (!(parms.target is Map map))
            {
                ModLogger.Error("TryExecuteWorker: parms.target is not a Map.");
                return false;
            }

            ModLogger.Message("ProceduralEvent triggered, capturing snapshot...");
            FireLlmEventAsync(map);
            return true;
        }

        private async void FireLlmEventAsync(Map map)
        {
            // Capture colony data once
            ColonySnapshot snapshot = ColonySnapshotCapturer.Capture(map);

            string saveId = SaveScopeComponent.Current?.SaveId;
            int gameTick = GenTicks.TicksGame;
            LLMCommunicationHandler com = new LLMCommunicationHandler();

            List<IEffect> chosenEvents = HeuristicEventSelector.Select(snapshot);

            string fullNarrative = SnapshotNarrator.Narrate(snapshot);

            string step2Prompt = ProperPromptGenerator.BuildSystemPrompt(
                    fullNarrative,
                    chosenEvents,
                    snapshot);

            string step2Provider = "unknown";
            string step2Model = "unknown";
            LlmParseResult<LlmCustomEvent> parseResult = null;
            string lastFailureReason = "Unknown error.";

            const int MaxAttempts = 3;
            const string RetryHint =
                "\n\nRETRY HINT: The previous response was rejected. You MUST include a non-empty '\"effects\"' array with at least one valid effect whose defName comes from the listed Allowed Values. Keep letterText to 2-3 short sentences, keep the whole response compact, and close every brace.";

            for (int attempt = 1; attempt <= MaxAttempts; attempt++)
            {
                if (attempt > 1)
                {
                    int backoffSeconds = 60;
                    string reasonPreview = lastFailureReason?.Length > 150 ? lastFailureReason.Substring(0, 150) + "..." : lastFailureReason;
                    ModLogger.Message($"[Step 2] Attempt {attempt}/{MaxAttempts}: backing off {backoffSeconds}s before retry (reason: {reasonPreview})");
                    await Task.Delay(TimeSpan.FromSeconds(backoffSeconds));
                }

                string attemptPrompt = attempt > 1 ? step2Prompt + RetryHint : step2Prompt;

                string responseRaw;
                int responsePromptTokens = 0;
                int responseCompletionTokens = 0;
                int responseReasoningTokens = 0;
                try
                {
                    LlmConnectionResult step2Result = await com.GetLlmResponseWithDetailsAsync(attemptPrompt);
                    if (step2Result == null || !step2Result.isSuccess)
                    {
                        lastFailureReason = step2Result?.errorMessage ?? "Unknown error";
                        step2Provider = step2Result?.providerId ?? "unknown";
                        step2Model = step2Result?.model ?? "unknown";
                        ModLogger.Error($"[Step 2] Attempt {attempt}/{MaxAttempts}: LLM call failed. Provider: {step2Provider}, Model: {step2Model}, Reason: {lastFailureReason}");
                        continue;
                    }

                    step2Provider = step2Result.providerId ?? "unknown";
                    step2Model = step2Result.model ?? "unknown";
                    responseRaw = step2Result.responseText;
                    responsePromptTokens = step2Result.promptTokens;
                    responseCompletionTokens = step2Result.completionTokens;
                    responseReasoningTokens = step2Result.reasoningTokens;
                }
                catch (Exception exception)
                {
                    lastFailureReason = exception.Message;
                    ModLogger.Error($"[Step 2] Attempt {attempt}/{MaxAttempts}: LLM call threw: {lastFailureReason}");
                    continue;
                }

                if (string.IsNullOrWhiteSpace(responseRaw))
                {
                    lastFailureReason = "The language model returned an empty response.";
                    ModLogger.Error($"[Step 2] Attempt {attempt}/{MaxAttempts}: {lastFailureReason}");
                    continue;
                }

                ModLogger.Message($"[Step 2] Attempt {attempt}/{MaxAttempts}: LLM response ({step2Provider}/{step2Model}, {responseRaw.Length} chars, P={responsePromptTokens} C={responseCompletionTokens} R={responseReasoningTokens}):\n{responseRaw}");

                try
                {
                    parseResult = LlmResponseParser.ParseEvent(responseRaw, chosenEvents);
                }
                catch (Exception exception)
                {
                    lastFailureReason = "Parser threw: " + exception.Message;
                    parseResult = null;
                    ModLogger.Error($"[Step 2] Attempt {attempt}/{MaxAttempts}: {lastFailureReason}");
                    continue;
                }

                if (!parseResult.isSuccess || parseResult.data == null)
                {
                    lastFailureReason = parseResult.errorMessage;
                    ModLogger.Error($"[Step 2] Attempt {attempt}/{MaxAttempts}: Parse failed. Provider: {step2Provider}, Model: {step2Model}, Reason: {lastFailureReason}\nPreview: {parseResult.responsePreview}");
                    continue;
                }

                break;
            }

            if (parseResult == null || !parseResult.isSuccess || parseResult.data == null)
            {
                ModLogger.Error($"[Step 2] All {MaxAttempts} attempts failed. Provider: {step2Provider}, Model: {step2Model}, Last reason: {lastFailureReason}");

                RimTelligenceDispatcher.ExecutionQueue.Enqueue(() =>
                {
                    Find.LetterStack.ReceiveLetter(
                        "Storyteller LLM Unavailable",
                        "The storyteller's language model could not produce a usable response after " + MaxAttempts + " attempts.\nProvider: " + step2Provider + "\nModel: " + step2Model + "\nLast error: " + lastFailureReason,
                        LetterDefOf.NegativeEvent);
                });
                return;
            }

            LlmCustomEvent finalEvent = parseResult.data;

            // Game Thread Execution
            RimTelligenceDispatcher.ExecutionQueue.Enqueue(() =>
            {
                GlobalTargetInfo target = LlmEffectExecutor.ExecuteAll(finalEvent, map);

                string summary = LlmEffectExecutor.BuildSummary(finalEvent);
                string fullText = $"{finalEvent.letterText ?? "Something happened."}\n\n{summary}";

                Find.LetterStack.ReceiveLetter(
                    finalEvent.letterTitle ?? "Strange Event",
                    fullText,
                    LetterDefOf.ThreatSmall, // may use different letter type for consideration
                    target.IsValid ? new LookTargets(target) : null
                );
            });
        }
    }
}