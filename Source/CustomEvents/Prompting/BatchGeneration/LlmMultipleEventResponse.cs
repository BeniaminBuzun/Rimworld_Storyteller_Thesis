using System.Collections.Generic;

namespace RimTelligence.Source.CustomEvents.Prompting.BatchGeneration
{
    internal class LlmMultipleEventResponse
    {
        public List<EventResponse> events { get; set; }
    }
    internal class EventResponse
    {
        public string eventName { get; set; }
        public string message { get; set; }

        public string runAfter { get; set; } 
    }
}
