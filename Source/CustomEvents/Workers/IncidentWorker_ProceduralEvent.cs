using Newtonsoft.Json;
using RimTelligence.Source.CustomEvents.EventSelector;
using RimTelligence.Source.CustomEvents.Execution;
using RimTelligence.Source.CustomEvents.Prompting;
using RimTelligence.Source.DataSnapshots;
using RimTelligence.Source.LlmCom;
using RimWorld;
using RimWorld.Planet;
using System;
using System.Collections.Generic;
using System.IO;
using Verse;

namespace RimTelligence.Source.CustomEvents.Workers
{
    // Wrapper class to deserialize the output of the First LLM call
    public class Step1ResponseWrapper
    {
        public List<SelectedEventIntent> selected_events { get; set; }
    }

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

            List<SelectedEventIntent> chosenEvents = HeuristicEventSelector.Select(snapshot, map);

            // Then feed directly into Step 2 as before



            // ====================================================================
            // STEP 2: STORY & PARAMETER GENERATION (Detailed Prompt)
            // ====================================================================

            string fullNarrative = SnapshotNarrator.Narrate(snapshot);
            string step2Prompt = ProperPromptGenerator.BuildSystemPrompt(
                    SnapshotNarrator.Narrate(snapshot),
                    chosenEvents,
                    snapshot);

            ModLogger.Message($"[Step 2] Prompt generated ({step2Prompt.Length} chars)");
            //TryWriteFile(@"C:\Users\Karol\Desktop\prompt_step2.txt", step2Prompt);

            string step2ResponseRaw;
            try
            {
                step2ResponseRaw = await com.GetLlmResponse(step2Prompt);
            }
            catch (Exception e)
            {
                ModLogger.Error($"[Step 2] LLM call failed: {e.Message}");
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
            catch (Exception e)
            {
                ModLogger.Error($"[Step 2] JSON parse failed: {e.Message}\nResponse was:\n{cleanedStep2Json}");
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
            catch (Exception e)
            {
                ModLogger.Error($"Failed to write log file to {path}: {e.Message}");
            }
        }
    }
}