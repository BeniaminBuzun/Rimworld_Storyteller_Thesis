using System.Collections.Generic;

namespace RimTelligence.Source.CustomEvents.Prompting
{
    internal class LlmMultipleEventResponse
    {
        public List<EventResponse> events { get; set; }
    }
    internal class EventResponse
    {
        public string eventName { get; set; }
        public string eventTitle { get; set; }
        public string eventDescription { get; set; }

        public int waitTime { get; set; }
    }
}
