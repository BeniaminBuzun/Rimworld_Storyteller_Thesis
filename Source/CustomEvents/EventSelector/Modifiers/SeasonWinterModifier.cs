using RimTelligence.DataSnapshots;

namespace RimTelligence.CustomEvents.EventSelector.Modifiers
{
    public class SeasonWinterModifier : IEventModifier
    {
        public float Modify(EventEntry entry, ColonySnapshot snapshot)
        {
            if (!(snapshot.season?.Contains("Winter") ?? false))
                return 1f;

            float multiplier = 1f;

            if (entry.Type == "ChangeWeather") multiplier *= 1.8f;
            if (entry.Type == "StartExtremeWeatherEvent") multiplier *= 1.2f;
            if (entry.Type.StartsWith("LoseItems") && entry.Type.Contains("Food")) multiplier *= 2f;

            return multiplier;
            
        }
    }
}