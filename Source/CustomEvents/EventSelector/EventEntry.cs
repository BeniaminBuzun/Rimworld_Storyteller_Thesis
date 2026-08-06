using RimTelligence.CustomEvents.Handlers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RimTelligence.CustomEvents.EventSelector
{
    public class EventEntry
    {
        public string Type;
        public float Weight;
        public EventAlignment Alignment;
        public string Family;  // "ResourcePodCrash", "LoseItems", etc.

        public EventEntry(string type, float weight, EventAlignment alignment)
        {
            Type = type;
            Weight = weight;
            Alignment = alignment;
            Family = type.Contains('_') ? type.Split('_')[0] : type;
        }
    }
}