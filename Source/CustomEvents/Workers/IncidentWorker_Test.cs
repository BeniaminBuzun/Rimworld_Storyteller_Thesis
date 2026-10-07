using RimTelligence.CustomEvents.Execution;
using RimTelligence.CustomEvents.Prompting;
using RimWorld;
using RimWorld.Planet;
using Verse;

namespace RimTelligence.CustomEvents.Workers
{
    public class IncidentWorker_Test : IncidentWorker
    {
        public void FireLlmEventAsync(Map map, string eventJson)
        {
            LlmParseResult<LlmCustomEvent> parseResult = LlmResponseParser.ParseEvent(eventJson);
            if (!parseResult.isSuccess || parseResult.data == null)
            {
                ModLogger.Error($"[Test Worker] Parse failed: {parseResult.errorMessage}\nPreview: {parseResult.responsePreview}");
                return;
            }

            LlmCustomEvent evt = parseResult.data;

            // 5. Game Thread Execution
            LongEventHandler.ExecuteWhenFinished(() =>
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