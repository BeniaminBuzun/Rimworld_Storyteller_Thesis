using RimTelligence.CustomEvents.Execution;
using RimTelligence.CustomEvents.Prompting;
using RimWorld;
using RimWorld.Planet;
using System.Collections.Generic;
using System.Linq;
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

            LongEventHandler.ExecuteWhenFinished(() =>
            {
                List<EffectOutcome> outcomes = LlmEffectExecutor.ExecuteAll(evt, map);

                string summary = LlmEffectExecutor.BuildSummary(evt);
                string fullText = $"{evt.letterText ?? "Something happened."}\n\n{summary}";

                List<GlobalTargetInfo> targets = outcomes.Select(o => o.Target).ToList();

                var letter = new ChoiceLetter_Procedural
                {
                    def = LetterDefOf.ThreatSmall,
                    ID = Find.UniqueIDsManager.GetNextLetterID(),
                    Label = evt.letterTitle ?? "Strange Event",
                    Text = fullText,
                    lookTargets = targets.Count > 0 ? new LookTargets(targets) : null,
                    jumpLabels = outcomes.Select(o => o.Label).ToList(),
                    jumpTargets = targets
                };

                Find.LetterStack.ReceiveLetter(letter);
            });
        }
    }
}