using RimWorld;
using Verse;
using RimTelligence.Source.LlmCom;

namespace RimTelligence.Source.CustomEvents
{
    public class IncidentWorker_CustomTextEvent : IncidentWorker
    {
        protected override bool TryExecuteWorker(IncidentParms parms)
        {
            FireLlmEvent();
            return true;
        }

        private async void FireLlmEvent()
        {
            BasicCom com = new BasicCom();
            string response = await com.GetLlmResponse(
                "Generate a short, dramatic in-universe RimWorld event message in one sentence."
            );

            string message = response ?? "Strange transmissions were intercepted but could not be decoded. (LLM com failed)";

            LongEventHandler.ExecuteWhenFinished(() =>
            {
                Find.LetterStack.ReceiveLetter(
                    "Transmission Received",
                    message,
                    LetterDefOf.NeutralEvent,
                    null,
                    null
                );
                Log.Message($"[RimTelligence] LLM event fired: {message}");
            });
        }
    }
}