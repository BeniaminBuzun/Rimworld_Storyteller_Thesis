using Newtonsoft.Json;
using RimTelligence.Source.CustomEvents.Execution;
using RimTelligence.Source.CustomEvents.Prompting;
using RimTelligence.Source.DataSnapshots;
using RimTelligence.Source.LlmCom;
using RimWorld;
using RimWorld.Planet;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;

// rewrite later nad DebugActions too written fastly with claude
namespace RimTelligence.Source.CustomEvents.Workers
{
    public class IncidentWorker_Test : IncidentWorker
    {
        public void FireLlmEventAsync(Map map, string eventJson )
        {
            LlmCustomEvent evt;

            evt = JsonConvert.DeserializeObject<LlmCustomEvent>(eventJson);

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
