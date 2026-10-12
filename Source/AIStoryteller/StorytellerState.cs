using RimTelligence.Llm;
using RimTelligence.Source.CustomEvents.Prompting;
using RimWorld;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using Verse;
namespace RimTelligence.Source.AIStoryteller
{
    internal class StorytellerState : GameComponent
    {
        private Queue<FiringIncident> incidentQueue = new Queue<FiringIncident>();

        public StorytellerState(Game game)
        {
            // Initialization code here
        }
        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref incidentQueue, "incidentQueue", new Queue<FiringIncident>());

            // Save/load state data here
        }
        public FiringIncident GetNextIncident(IIncidentTarget target, StorytellerComp comp)
        {
            if (incidentQueue.Count == 0)
            {
                generateNewEvents(target, comp);
                ModLogger.Message($"incidents left: {incidentQueue.Count}");

            }
            return incidentQueue.Dequeue();

        }
        private void generateNewEvents(IIncidentTarget target, StorytellerComp comp)
        {
            ModLogger.Message($"generating new events");
            LlmCall(target);
        }
        private async void LlmCall(IIncidentTarget target)
        {
            //create a list of human readable labels of events
            List<string> defLabels = DefDatabase<IncidentDef>.AllDefsListForReading
            .Where(def => def.targetTags != null && def.targetTags.Contains(IncidentTargetTagDefOf.Map_PlayerHome))
            .Select(def => "name: " + def.label+ ", description: " + def.description)
            .ToList();

            //create a dictionary of event labels and their corresponding defNames
            Dictionary<string, string> defDict = DefDatabase<IncidentDef>.AllDefsListForReading
            .Where(def => !string.IsNullOrEmpty(def.label)) //delete missing labels
            .Where(def => def.targetTags != null && def.targetTags.Contains(IncidentTargetTagDefOf.Map_PlayerHome)) //allow only events that can be used on player home map
            .GroupBy(def => def.label) //save to dict
            .ToDictionary(
                group => group.Key,
                group => group.First().defName
            );

            string prompt = "You ara a rimworld storyteller. Create short self-contained story consisting multiple consecutive events. Events should work together as a story connecting one to the other. Story should have a clear beginning, middle, and end. Avoid repetitive or predictable content. Resolve all mysteries you create. Use only available events.\r\n" +
            "Use only available events. Available events: " + string.Join(", ", defLabels);

            ModLogger.Message($"LLM request sent");
            ModLogger.Message($"{prompt}");

            LLMCommunicationHandler com = new LLMCommunicationHandler();
            LlmConnectionResult llmResult = await com.GetLlmResponseWithDetailsAsync(prompt);
            if (llmResult == null || !llmResult.isSuccess)
            // in case there is an error
            {
                ModLogger.Message($"LLM error");

            }
            //string step2Provider = llmResult?.providerId ?? "unknown";
            //string step2Model = llmResult?.model ?? "unknown";
            string responseRaw = llmResult.responseText;
            
            ModLogger.Message($"llm 1st stage response: {responseRaw}");
            string conversionPrompt = "Convert following rimworld events to JSON format: {events:[ {eventName: <name of event>, message:<message displayed when executing event>, waitTime:<how many hours should pass between last event and this one>} ] }\r\n"+
                "Available events: " + string.Join(", ", defLabels) + "\r\n" +
                "story: " + responseRaw;



            LlmConnectionResult llmConversionResult = await com.GetLlmResponseWithDetailsAsync(conversionPrompt);
            ModLogger.Message($"llm 2nd stage response: {llmConversionResult.responseText}");

            LlmMultipleEventResponse receivedData = JsonSerializer.Deserialize<LlmMultipleEventResponse>(llmConversionResult.responseText);

            for (int i = 0; i < receivedData.events.Count; i++)
            {
                var eventData = receivedData.events[i];
                ModLogger.Message($"event label {i}: {eventData.eventName}");
                string defName = defDict.TryGetValue(eventData.eventName, null);
                ModLogger.Message($"event name{i}: {defName}");

                if (defName != null)
                {
                    IncidentDef incidentDef = DefDatabase<IncidentDef>.GetNamed(defDict[eventData.eventName]);

                    IncidentParms parms = new IncidentParms
                    {
                        target = target,
                        customLetterLabel = eventData.eventTitle,
                        customLetterText = eventData.eventDescription,
                        forced = true,
                    };
                    FiringIncident newIncident = new FiringIncident(incidentDef, null, parms);
                    incidentQueue.Enqueue(newIncident);
                }
                else
                {
                    ModLogger.Message($"Unknown event name: {eventData.eventName}");
                }
            }
        }
    }
}
