using LlmTornado.Chat;
using RimTelligence.CustomEvents.Prompting;
using RimTelligence.DataSnapshots;
using RimTelligence.Llm;
using RimTelligence.Source.CustomEvents.Prompting;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using Verse;
using Verse.Noise;
namespace RimTelligence.Source.AIStoryteller
{
    internal class StorytellerState : GameComponent
    {
        private Queue<FiringIncident> incidentQueue = new Queue<FiringIncident>();
        private Conversation generationChat = null;
        private Conversation conversionChat = null;

        public StorytellerState(Game game)
        {
            // Initialization code here
        }
        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref incidentQueue, "incidentQueue", new Queue<FiringIncident>());
            Scribe_Values.Look(ref generationChat, "generationChat", null);
            Scribe_Values.Look(ref conversionChat, "conversionChat", null);

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
            if (target is Map map)
            {
                LlmCall(map);

            }

        }
        private async void LlmCall(Map target)
        {
            //Cretion
            List<string> defLabels = DefDatabase<IncidentDef>.AllDefsListForReading
            .Where(def => def.targetTags != null && def.targetTags.Contains(IncidentTargetTagDefOf.Map_PlayerHome))
            .Select(def => "name: " + def.label + ", description: " + def.description)
            .ToList();

            //Conversation for generating new events
            if (generationChat == null)
            {
                ModLogger.Message($"Starting new conversation");
                LLMCommunicationHandler com = new LLMCommunicationHandler();
                generationChat = com.StartNewConversation();

                string systemPrompt = "You ara a rimworld storyteller. Create short self-contained story consisting multiple consecutive events. Events should work together as a story connecting one to the other. Story should have a clear beginning, middle, and end. Avoid repetitive or predictable content. Resolve all mysteries you create. Use only available events.\r\n" +
                "Use only available events. Available events: " + string.Join(", ", defLabels);
                generationChat.AppendSystemMessage(systemPrompt);


            }
            //Conversation for converting events to JSON
            if (conversionChat == null)
            {
                ModLogger.Message($"Starting new conversation");
                LLMCommunicationHandler com = new LLMCommunicationHandler();
                conversionChat = com.StartNewConversation();
                string conversionPrompt = "Convert recived events to Json format.\r\n" +
                "Return output ONLY in JSON format: {events:[ {eventName: <name of event>,eventTitle:<title of event>, eventDescription:<message displayed when executing event>, waitTime:<how many hours should pass between last event and this one>} ] }\r\n" +
                "Use only available events. Available events: " + string.Join(", ", defLabels);
                conversionChat.AppendSystemMessage(conversionPrompt);

            }



            //ColonySnapshot snapshot = ColonySnapshotCapturer.Capture(target);

            //ProperPromptGenerator promptGenerator = new ProperPromptGenerator();
            //create a dictionary of event labels and their corresponding defNames
            Dictionary<string, string> defDict = DefDatabase<IncidentDef>.AllDefsListForReading
            .Where(def => !string.IsNullOrEmpty(def.label)) //delete missing labels
            .Where(def => def.targetTags != null && def.targetTags.Contains(IncidentTargetTagDefOf.Map_PlayerHome)) //allow only events that can be used on player home map
            .GroupBy(def => def.label) //save to dict
            .ToDictionary(
                group => group.Key,
                group => group.First().defName
            );



            //LLMCommunicationHandler com = new LLMCommunicationHandler();
            //LlmConnectionResult llmResult = await com.GetLlmResponseWithDetailsAsync(prompt);
            //if (llmResult == null || !llmResult.isSuccess)
            // in case there is an error
            //{
            //ModLogger.Message($"LLM error");

            //}
            //string step2Provider = llmResult?.providerId ?? "unknown";
            //string step2Model = llmResult?.model ?? "unknown";
            //generationChat.AppendUserInput("generate events");

            string responseRaw = await generationChat.GetResponse();
            
            ModLogger.Message($"llm 1st stage response: {responseRaw}");

            conversionChat.AppendUserInput(responseRaw);

            string llmConversionResult = await conversionChat.GetResponse();
            ModLogger.Message($"llm 2nd stage response: {llmConversionResult}");

            LlmMultipleEventResponse receivedData = JsonSerializer.Deserialize<LlmMultipleEventResponse>(llmConversionResult);

            for (int i = 0; i < receivedData.events.Count; i++)
            {
                var eventData = receivedData.events[i];
                ModLogger.Message($"event label {i}: {eventData.eventName}");
                string defName = defDict.TryGetValue(eventData.eventName, null);
                ModLogger.Message($"event name{i}: {defName}");

                if (defName != null)
                {
                    IncidentDef incidentDef = DefDatabase<IncidentDef>.GetNamed(defDict[eventData.eventName]);
                    Find.WindowStack.Add(new Dialog_MessageBox(
                        text: eventData.eventDescription ,
                        title: eventData.eventTitle
                    ));

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
