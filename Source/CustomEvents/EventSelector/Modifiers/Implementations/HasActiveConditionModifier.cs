
using RimTelligence.Source.DataSnapshots;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RimTelligence.Source.CustomEvents.EventSelector.Modifiers.Implementations
{
    public class HasActiveConditionModifier : IEventModifier
    {

        public float Modify(EventEntry entry, ColonySnapshot snapshot)
        {
            float multiplier = 1f;
            if (entry.Type == "StartExtremeWeatherEvent") multiplier *= 0.2f;
            if (entry.Type == "ChangeWeather") multiplier *= 0.5f;

            return multiplier;
            
        }

        public bool ShouldApply(ColonySnapshot snapshot) => snapshot.activeConditions?.Count > 0;
    }
}
