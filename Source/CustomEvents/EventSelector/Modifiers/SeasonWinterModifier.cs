using RimTelligence.CustomEvents.Handlers;
using RimTelligence.DataSnapshots;

namespace RimTelligence.CustomEvents.EventSelector.Modifiers
{
    public class SeasonWinterModifier : IEventModifier
    {
        public float Modify(IEffect entry, ColonySnapshot snapshot)
        {
            if (!(snapshot.season?.Contains("Winter") ?? false))
                return 1f;

            float multiplier = 1f;

            if (entry.Config.EffectName == "ChangeWeather") multiplier *= 1.8f;
            if (entry.Config.EffectName == "StartExtremeWeatherEvent") multiplier *= 1.2f;
            return multiplier;
            
        }
    }
}