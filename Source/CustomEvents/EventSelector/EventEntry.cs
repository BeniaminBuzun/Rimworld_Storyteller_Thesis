using RimTelligence.CustomEvents.Handlers;

namespace RimTelligence.CustomEvents.EventSelector
{
    public class EventEntry
    {
        public string Type;
        public float Weight;
        public EventAlignment Alignment;

        public EventEntry(string type, float weight, EventAlignment alignment)
        {
            Type = type;
            Weight = weight;
            Alignment = alignment;
        }
    }
}