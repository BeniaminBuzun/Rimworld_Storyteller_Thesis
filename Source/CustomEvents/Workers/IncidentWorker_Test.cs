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

                string summary = LlmEffectExecutor.BuildSummary(evt, outcomes);
                string fullText = $"{evt.letterText ?? "Something happened."}\n\n{summary}";

                List<GlobalTargetInfo> validTargets = outcomes.Where(o => o.Target.IsValid).Select(o => o.Target).ToList();

                var letter = new ChoiceLetter_Procedural
                {
                    def = LetterDefOf.ThreatSmall,
                    ID = Find.UniqueIDsManager.GetNextLetterID(),
                    Label = evt.letterTitle ?? "Strange Event",
                    Text = fullText,
                    lookTargets = validTargets.Count > 0 ? new LookTargets(validTargets) : null,
                    jumpLabels = outcomes.Select(o => o.Label).ToList(),
                    jumpTargets = outcomes.Select(o => o.Target).ToList()
                };

                Find.LetterStack.ReceiveLetter(letter);
            });
        }
    }
}