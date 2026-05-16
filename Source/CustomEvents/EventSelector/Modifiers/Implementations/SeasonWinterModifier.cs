using RimTelligence.Source.DataSnapshots;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RimTelligence.Source.CustomEvents.EventSelector.Modifiers.Implementations
{
    public class SeasonWinterModifier : IEventModifier
    {
        public float Modify(EventEntry entry, ColonySnapshot snapshot)
        {
            float multiplier = 1f;

            if (entry.Type == "ChangeWeather") multiplier *= 1.8f;
            if (entry.Type == "StartExtremeWeatherEvent") multiplier *= 1.2f;
            if (entry.Family == "LoseItems" && entry.Type.Contains("Food")) multiplier *= 2f;

            return multiplier;
            
        }

        public bool ShouldApply(ColonySnapshot snapshot)
        {
            return snapshot.season?.Contains("Winter") ?? false;
        }
    }
}
