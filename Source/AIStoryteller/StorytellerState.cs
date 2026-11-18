using LlmTornado.Chat;
using RimTelligence.AIStoryteller;
using RimTelligence.DataSnapshots;
using RimTelligence.Llm;
using RimTelligence.Source.CustomEvents.Prompting.BatchGeneration;
using RimTelligence.Source.DataSnapshots.Triggers;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Verse;
namespace RimTelligence.Source.AIStoryteller
{
    internal class StorytellerState : GameComponent
    {
        private Conversation llmStoryGenerator;
        private Conversation llmStoryConverter;
        private Trigger trigger = null;
        private Queue<FiringIncident> incidentQueue = new Queue<FiringIncident>();
        private Queue<EventResponse> incidentData = new Queue<EventResponse>();
        private int ticksAfterLastIncident = 0;
        private int delayBeforeNextIncident = 0;
        public StorytellerState(Game game)
        {
        }
        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref incidentQueue, "incidentQueue", new Queue<FiringIncident>());
            Scribe_Values.Look(ref incidentData, "incidentData", new Queue<EventResponse>());
            Scribe_Values.Look(ref ticksAfterLastIncident, "ticksAfterLastIncident", 0);
            Scribe_Values.Look(ref delayBeforeNextIncident, "delayBeforeNextIncident", 0);

            // Save/load state data here
        }
        public bool shouldFireIncident(IIncidentTarget target)
        {
            ticksAfterLastIncident += 1000;
            if (incidentQueue.Count == 0)
            {
                generateNewEvents(target, null);
                return false;
            }
            if (ticksAfterLastIncident < delayBeforeNextIncident)
            {
                ModLogger.Message($"Waiting for delay: {ticksAfterLastIncident} ticks elapsed");
                return false;
            }
            if (trigger != null)
            {
                if (trigger.RunAfter(target as Map))
                {
                    ModLogger.Message($"Trigger condition met, firing incident");
                    trigger = null;
                    return true;
                }
                else
                {
                    ModLogger.Message($"Trigger condition not met, waiting for next check");
                    return false;
                }
            }
            return true;
        }


        public FiringIncident GetNextIncident(IIncidentTarget target, StorytellerComp comp)
        {
            if (incidentQueue.Count == 0)
            {
                generateNewEvents(target, comp);

            }
            ModLogger.Message($"incidents left: {incidentQueue.Count}");


            var currentlyRunIncidentData = incidentData.Dequeue();
            //ModLogger.Message($"check for triggers: {currentlyRunIncidentData.runAfter}");
            if (currentlyRunIncidentData.runAfter == "longDelay")
            {
                delayBeforeNextIncident = 60000; // 60 seconds
            }
            else if (currentlyRunIncidentData.runAfter == "shortDelay")
            {
                delayBeforeNextIncident = 10000; // 10 seconds
            }
            else
            {
                delayBeforeNextIncident = 0;
            }
            //else if (currentlyRunIncidentData.runAfter == "customTrigger")
            //{
            //    delayBeforeNextIncident = 0; // No delay, wait for trigger
            //}
            //else
            //{
            //    ModLogger.Message($"Unknown runAfter value: {currentlyRunIncidentData.runAfter}, defaulting to shortDelay");
            //    delayBeforeNextIncident = 10000; // Default to short delay
            //})

            //if (currentlyRunIncidentData.runAfter.Contains("join"))
            //{
            //    ModLogger.Message($"Trigger placed");
            //    trigger = new DetectIfJoined();
            //    trigger.RunBefore(target as Map);
            //}
            ticksAfterLastIncident = 0;
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
        public async Task<ChatRichResponse> GetResponseWithRetryAsync(Conversation lmcom,
            int maxAttempts = 3,
        int initialDelayMs = 10000,
        CancellationToken ct = default)
            {
                int delayMs = initialDelayMs;

                for (int attempt = 1; ; attempt++)
                {
                    try
                    {
                        ChatRichResponse response = await lmcom.GetResponseRich();

                        // Optional: validate the result, since a call can succeed without throwing
                        if (response is null /* && response.IsValid */)

                    {
                        Console.WriteLine($"LLM error, retrying...");

                    }
                    else
                    {
                        return response;
                    }

                    Console.WriteLine($"Attempt {attempt}: response was null or invalid.");
                    }
                    catch (Exception ex) when (attempt < maxAttempts)
                    {
                        Console.WriteLine($"Attempt {attempt} failed: {ex.Message}");
                    }

                    if (attempt >= maxAttempts)
                    {
                        throw new InvalidOperationException(
                            $"GetResponseRich failed after {maxAttempts} attempts.");
                    }

                    await Task.Delay(delayMs, ct);
                    //delayMs *= 2; // exponential backoff
                }
            }

        private async void LlmCall(Map target)
        {

            //ColonySnapshot snapshot = ColonySnapshotCapturer.Capture(target);

            //create a list of human readable labels of events
            List<string> defNames = DefDatabase<IncidentDef>.AllDefsListForReading
            .Where(def => def.targetTags != null && def.targetTags.Contains(IncidentTargetTagDefOf.Map_PlayerHome))
            .Where(def => EventData.descriptions.ContainsKey(def.label)) //allow only events that can be used on player home map
            .Select(def => "name: " + def.label+ "  " + (EventData.descriptions.TryGetValue(def.label, out EventInfo desc) ? desc.getConditions() : ""))
            .ToList();

            List<string> defLabels = DefDatabase<IncidentDef>.AllDefsListForReading
            .Where(def => def.targetTags != null && def.targetTags.Contains(IncidentTargetTagDefOf.Map_PlayerHome))
            .Where(def => EventData.descriptions.ContainsKey(def.label)) //allow only events that can be used on player home map

            .Select(def => "name: " + def.label + ", description: " + (EventData.descriptions.TryGetValue(def.label, out EventInfo desc) ? desc.Description + ", severity: " + desc.Severity : "No description available") )
            .ToList();

            //create a dictionary of event labels and their corresponding defNames
            Dictionary<string, string> defDict = DefDatabase<IncidentDef>.AllDefsListForReading
            .Where(def => !string.IsNullOrEmpty(def.label)) //delete missing labels
            .Where(def => def.targetTags != null && def.targetTags.Contains(IncidentTargetTagDefOf.Map_PlayerHome)) //allow only events that can be used on player home map
            .Where(def => EventData.descriptions.ContainsKey(def.label)) //allow only events that can be used on player home map

            .GroupBy(def => def.label) //save to dict
            .ToDictionary(
                group => group.Key,
                group => group.First().defName
            );
            string generationPrompt = "You ara a rimworld storyteller. Create short self-contained story consisting multiple consecutive events. Events should work together as a story connecting one to the other. Story should have a clear beginning, middle, and end. Avoid repetitive content. Avid Resolve all mysteries you create. The world is harsh and unforgiving place, your story should reflect that. Use only available events.\r\n" +
            "Use only available events. Available events: " + string.Join(", ", defLabels);

            string conversionPrompt = "Convert following rimworld events to JSON format: {events:[ {eventName: <name of event>, message:<message displayed when executing event>, runAfter: <how soon should next event occur>: longDelay, shortDelay, or custom trigger. Some events have custom triggers, use them when availible> } ] }\r\n" +
            "Available events: " + string.Join(", ", defNames) + "\r\n";


            LLMCommunicationHandler com = new LLMCommunicationHandler();
            //if (llmStoryGenerator == null)
            //{

            llmStoryGenerator = await com.StartNewConversation();
            llmStoryGenerator.AppendSystemMessage(generationPrompt);
            //}
            //if (llmStoryConverter == null)
            //{

            llmStoryConverter = await com.StartNewConversation();
            llmStoryConverter.AppendSystemMessage(conversionPrompt);
            //}
            llmStoryGenerator.AddMessage(new ChatMessage { Role = LlmTornado.Code.ChatMessageRoles.User, Content = "Generate new story" });
            
            ChatRichResponse response = await GetResponseWithRetryAsync(llmStoryGenerator);
            //ChatRichResponse response = await llmStoryGenerator.GetResponseRich();

            ModLogger.Message($"LLM request sent");
            ModLogger.Message($"{generationPrompt}");
            string story = response.Text;
            ModLogger.Message($"llm 1st stage response: {story}");

            llmStoryConverter.AddMessage(new ChatMessage { Role = LlmTornado.Code.ChatMessageRoles.User, Content = story });
            //ChatRichResponse response2 = await llmStoryConverter.GetResponseRich();
            ChatRichResponse response2 = await GetResponseWithRetryAsync(llmStoryConverter); 
            string jsonPlainText = response2.Text;
            ModLogger.Message($"{conversionPrompt}");


            int startIndex = jsonPlainText.IndexOf('{');
            int endIndex = jsonPlainText.LastIndexOf('}');
            if (startIndex >= 0 && endIndex > startIndex)
            {
                // 2. Extract only the clean JSON substring between them
                jsonPlainText = jsonPlainText.Substring(startIndex, (endIndex - startIndex) + 1);
            }
            jsonPlainText = jsonPlainText.Replace('\u2011', '-').Replace('\u2013', '-').Replace('\u2014', '-');
            ModLogger.Message($"llm 2nd stage response: {jsonPlainText}");

            LlmMultipleEventResponse receivedData = JsonSerializer.Deserialize<LlmMultipleEventResponse>(jsonPlainText);
            
            if (receivedData == null || receivedData.events == null || receivedData.events.Count == 0)
            {
                ModLogger.Error($"Failed to deserialize LLM response or no events found.");
                return;
            }

            for (int i = 0; i < receivedData.events.Count; i++)
            {


                var eventData = receivedData.events[i];
                
                string defName = defDict.TryGetValue(eventData.eventName, null);
                ModLogger.Message($"event name{i}: {defName}");
                ModLogger.Message($"event label {i}: {eventData.eventName}");

                if (defName != null) 
                {
                    IncidentDef incidentDef = DefDatabase<IncidentDef>.GetNamed(defDict[eventData.eventName]);

                    IncidentParms parms = new IncidentParms
                    {
                        target = target,
                        customLetterLabel = eventData.eventName,
                        customLetterText = eventData.message,
                        forced = true,
                    };

                    FiringIncident newIncident = new FiringIncident(incidentDef, null, parms);
                    incidentQueue.Enqueue(newIncident);
                    incidentData.Enqueue(eventData);

                }

                else
                {
                    ModLogger.Message($"Unknown event name: {eventData.eventName}");
                }
            }
        }
    }
}