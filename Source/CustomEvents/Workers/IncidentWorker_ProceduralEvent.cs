using System;
using Newtonsoft.Json;
using RimWorld;
using RimWorld.Planet;
using Verse;
using RimTelligence.Source.DataSnapshots;
using RimTelligence.Source.LlmCom;
using RimTelligence.Source.CustomEvents.Execution;
using RimTelligence.Source.CustomEvents.Prompting;

namespace RimTelligence.Source.CustomEvents.Workers
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
            // 1. Data Collection
            ColonySnapshot snapshot = ColonySnapshotCapturer.Capture(map);
            string snapshotJson = JsonConvert.SerializeObject(snapshot, Formatting.Indented);

            string snapshotNarrative = SnapshotNarrator.Narrate(snapshot);

            // 2. Prompt Generation
            string prompt = PromptGenerator.BuildSystemPrompt(snapshotNarrative);
            ModLogger.Message($"Prompt generated: {prompt}");
            ModLogger.Message($"Sending prompt to LLM ({prompt.Length} chars)");

            // 3. External API Call
            BasicCom com = new BasicCom();
            string response;
            try
            {
                response = await com.GetLlmResponse(prompt);
            }
            catch (Exception e)
            {
                ModLogger.Error($"LLM call failed: {e.Message}");
                return;
            }

            if (string.IsNullOrWhiteSpace(response))
            {
                ModLogger.Error("LLM returned empty response.");
                return;
            }
            ModLogger.Message($"LLM response received: {response}");

            // 4. Data Parsing & Sanitization
            string cleanedJson = response.Replace("```json", "").Replace("```", "").Trim();
            LlmCustomEvent evt;

            try
            {
                evt = JsonConvert.DeserializeObject<LlmCustomEvent>(cleanedJson);
            }
            catch (Exception e)
            {
                ModLogger.Error($"JSON parse failed: {e.Message}\nResponse was:\n{cleanedJson}");
                return;
            }

            if (evt == null)
            {
                ModLogger.Error($"JSON deserialized to null.\nResponse was:\n{cleanedJson}");
                return;
            }

            // 4b. Strip effects the LLM emitted incorrectly (missing defName, all-zero, unknown type)
            evt.effects = LlmEffectValidator.StripInvalid(evt.effects);



            // 5. Game Thread Execution
            RimTelligenceDispatcher.ExecutionQueue.Enqueue(() =>
            {
                GlobalTargetInfo target = LlmEffectExecutor.ExecuteAll(evt, map);

                string summary = LlmEffectExecutor.BuildSummary(evt);
                string fullText = $"{evt.letterText ?? "Something happened."}\n\n{summary}";

                Find.LetterStack.ReceiveLetter(
                    evt.letterTitle ?? "Strange Event",
                    fullText,
                    LetterDefOf.ThreatSmall,
                    target.IsValid ? new LookTargets(target) : null
                );
            });
        }
    }
}