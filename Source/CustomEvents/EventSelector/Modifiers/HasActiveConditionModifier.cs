
using RimTelligence.DataSnapshots;

namespace RimTelligence.CustomEvents.EventSelector.Modifiers
{
    public class HasActiveConditionModifier : IEventModifier
    {

        public float Modify(EventEntry entry, ColonySnapshot snapshot)
        {
            if (!(snapshot.activeConditions?.Count > 0))
                return 1f;

            float multiplier = 1f;
            if (entry.Type == "StartExtremeWeatherEvent") multiplier *= 0.2f;
            if (entry.Type == "ChangeWeather") multiplier *= 0.5f;

            return multiplier;
            
        }
    }
}