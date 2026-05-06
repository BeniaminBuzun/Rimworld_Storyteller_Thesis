using Newtonsoft.Json;
using RimTelligence.Source.DataSnapshots;
using RimTelligence.Source.LlmCom;
using RimWorld;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Verse;
using Verse.Noise;

namespace RimTelligence.Source.CustomEvents
{
    public class IncidentWorker_CustomTextEvent : IncidentWorker
    {
        protected override bool TryExecuteWorker(IncidentParms parms)
        {
            //FireLlmEvent();
            var snapshot = ColonySnapshotCapturer.Capture(Find.CurrentMap);
            string json = JsonConvert.SerializeObject(snapshot, Formatting.Indented);
            File.WriteAllText(@"C:\Users\Karol\Desktop\quickdump.json", json);
            Map map = Find.CurrentMap;
            var test = map.resourceCounter.TotalHumanEdibleNutrition;
            Log.Message(test.ToString());
            var sb = new StringBuilder();

            void Append(string label, List<string> list)
            {
                sb.AppendLine(label);
                sb.AppendLine(list.Count == 0 ? "  (empty)" : string.Join(", ", list));
                sb.AppendLine();
            }

            Append("weapons", DataList.WeaponList);
            Append("apparel", DataList.ApparelList);
            Append("animals", DataList.AnimalList);
            Append("food", DataList.FoodList);
            Append("medicine", DataList.MedicineList);
            Append("drugs", DataList.DrugsList);
            Append("diseases", DataList.DiseaseList);
            Append("resources", DataList.ResourceList);
            Append("conditions", DataList.GameConditionList);
            Append("weathers", DataList.AllWeathersList);

            Log.Message(sb.ToString());

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