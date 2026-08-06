using Newtonsoft.Json;
using RimTelligence.AIStoryteller;
using RimTelligence.CustomEvents.EventSelector;
using RimTelligence.CustomEvents.Execution;
using RimTelligence.CustomEvents.Prompting;
using RimTelligence.DataSnapshots;
using RimTelligence.Llm;
using RimTelligence.Memory;
using RimWorld;
using RimWorld.Planet;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Verse;

namespace RimTelligence.CustomEvents.Workers
{
    // Wrapper class to deserialize the output of the First LLM call
    public class Step1ResponseWrapper
    {
        public List<SelectedEventIntent> selected_events { get; set; }
    }

    public class IncidentWorker_ProceduralEvent : IncidentWorker
    {
        private static RagContextProvider _ragProvider;
        private static bool _ragProviderInitialized;

        private static RagContextProvider GetOrCreateRagProvider()
        {
            if (!_ragProviderInitialized)
            {
                try
                {
                    _ragProvider = new RagContextProvider(new OnnxEmbeddingProvider());
                }
                catch (Exception exception)
                {
                    ModLogger.Error($"[RAG] Failed to initialize OnnxEmbeddingProvider, falling back to stub: {exception.Message}");
                    _ragProvider = new RagContextProvider(new StubEmbeddingProvider());
                }

                var settings = AIStorytellerMod.Settings;
                _ragProvider.Configure(settings.ragMaxResultsPerStore, settings.ragMinSimilarity);
                _ragProviderInitialized = true;
            }
            return _ragProvider;
        }

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
            BasicCom com = new BasicCom();

            // commented for now llm was able to chose events but it was boring and difficult to force fun events 

            // ====================================================================
            // STEP 1: EVENT SELECTION (Brief Prompt)
            // ====================================================================

            //string briefNarrative = SnapshotNarrator.Narrate(snapshot);
            //string step1Prompt = InitialPromptGenerator.BuildSystemPrompt(briefNarrative);

            //ModLogger.Message($"[Step 1] Prompt generated ({step1Prompt.Length} chars)");
            //TryWriteFile(@"C:\Users\Karol\Desktop\prompt_step1.txt", step1Prompt);

            //string step1ResponseRaw;
            //try
            //{
            //    step1ResponseRaw = await com.GetLlmResponse(step1Prompt);
            //}
            //catch (Exception e)
            //{
            //    ModLogger.Error($"[Step 1] LLM call failed: {e.Message}");
            //    return;
            //}

            //if (string.IsNullOrWhiteSpace(step1ResponseRaw))
            //{
            //    ModLogger.Error("[Step 1] LLM returned empty response.");
            //    return;
            //}

            //ModLogger.Message($"[Step 1] Response received.");
            //TryWriteFile(@"C:\Users\Karol\Desktop\response_step1.txt", step1ResponseRaw);

            //// Parse Step 1 Data
            //string cleanedStep1Json = step1ResponseRaw.Replace("```json", "").Replace("```", "").Trim();
            //Step1ResponseWrapper step1Intent;
            //try
            //{
            //    step1Intent = JsonConvert.DeserializeObject<Step1ResponseWrapper>(cleanedStep1Json);
            //}
            //catch (Exception e)
            //{
            //    ModLogger.Error($"[Step 1] JSON parse failed: {e.Message}\nResponse was:\n{cleanedStep1Json}");
            //    return;
            //}

            //if (step1Intent?.selected_events == null || step1Intent.selected_events.Count == 0)
            //{
            //    ModLogger.Error("[Step 1] JSON deserialized to null or contained no events.");
            //    return;
            //}

            List<SelectedEventIntent> chosenEvents = HeuristicEventSelector.Select(snapshot);

            // Then feed directly into Step 2 as before


            // ====================================================================
            // RAG CONTEXT QUERY
            // ====================================================================

            string fullNarrative = SnapshotNarrator.Narrate(snapshot);
            RagQueryResult ragContext = null;
            if (AIStorytellerMod.Settings.ragEnabled)
            {
                try
                {
                    ragContext = await GetOrCreateRagProvider().QueryAsync(fullNarrative);
                    if (ragContext.HasResults)
                        ModLogger.Message($"[RAG] Context loaded: {ragContext.StaticResults.Count} static + {ragContext.DynamicResults.Count} dynamic results");
                }
                catch (Exception exception)
                {
                    ModLogger.Warning($"[RAG] Query failed (non-fatal): {exception.Message}");
                }
            }

            // ====================================================================
            // STEP 2: STORY & PARAMETER GENERATION (Detailed Prompt)
            // ====================================================================

            string step2Prompt = ProperPromptGenerator.BuildSystemPrompt(
                    fullNarrative,
                    chosenEvents,
                    snapshot,
                    ragContext);

            ModLogger.Message($"[Step 2] Prompt generated ({step2Prompt.Length} chars)");
            //TryWriteFile(@"C:\Users\Karol\Desktop\prompt_step2.txt", step2Prompt);

            string step2ResponseRaw;
            try
            {
                step2ResponseRaw = await com.GetLlmResponse(step2Prompt);
            }
            catch (Exception exception)
            {
                ModLogger.Error($"[Step 2] LLM call failed: {exception.Message}");
                return;
            }

            if (string.IsNullOrWhiteSpace(step2ResponseRaw))
            {
                ModLogger.Error("[Step 2] LLM returned empty response.");
                return;
            }

            ModLogger.Message($"[Step 2] Response received.");
            //TryWriteFile(@"C:\Users\Karol\Desktop\response_step2.txt", step2ResponseRaw);

            // Parse Step 2 Data
            string cleanedStep2Json = step2ResponseRaw.Replace("```json", "").Replace("```", "").Trim();
            LlmCustomEvent finalEvent;

            try
            {
                finalEvent = JsonConvert.DeserializeObject<LlmCustomEvent>(cleanedStep2Json);
            }
            catch (Exception exception)
            {
                ModLogger.Error($"[Step 2] JSON parse failed: {exception.Message}\nResponse was:\n{cleanedStep2Json}");
                return;
            }

            if (finalEvent == null)
            {
                ModLogger.Error($"[Step 2] JSON deserialized to null.\nResponse was:\n{cleanedStep2Json}");
                return;
            }

            // ====================================================================
            // EXECUTION
            // ====================================================================

            // Strip effects the LLM emitted incorrectly (missing defName, all-zero, unknown type)
            finalEvent.effects = LlmEffectValidator.StripInvalid(finalEvent.effects);

            // Game Thread Execution
            RimTelligenceDispatcher.ExecutionQueue.Enqueue(() =>
            {
                GlobalTargetInfo target = LlmEffectExecutor.ExecuteAll(finalEvent, map);

                string summary = LlmEffectExecutor.BuildSummary(finalEvent);
                string fullText = $"{finalEvent.letterText ?? "Something happened."}\n\n{summary}";

                Find.LetterStack.ReceiveLetter(
                    finalEvent.letterTitle ?? "Strange Event",
                    fullText,
                    LetterDefOf.ThreatSmall,
                    target.IsValid ? new LookTargets(target) : null
                );
            });
        }

        // Helper method to write files safely without crashing the worker if the path is locked/invalid
        private void TryWriteFile(string path, string content)
        {
            try
            {
                File.WriteAllText(path, content);
            }
            catch (Exception exception)
            {
                ModLogger.Error($"Failed to write log file to {path}: {exception.Message}");
            }
        }
    }
}