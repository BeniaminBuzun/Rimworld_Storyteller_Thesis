
using RimTelligence.CustomEvents.Handlers;
using RimTelligence.DataSnapshots;

namespace RimTelligence.CustomEvents.EventSelector.Modifiers
{
    public class HasActiveConditionModifier : IEventModifier
    {

        public float Modify(IEffect entry, ColonySnapshot snapshot)
        {
            if (!(snapshot.activeConditions?.Count > 0))
                return 1f;

            float multiplier = 1f;
            if (entry.Config.EffectName == "StartExtremeWeatherEvent") multiplier *= 0.2f;
            if (entry.Config.EffectName == "ChangeWeather") multiplier *= 0.5f;

            return multiplier;
            
        }
    }
}