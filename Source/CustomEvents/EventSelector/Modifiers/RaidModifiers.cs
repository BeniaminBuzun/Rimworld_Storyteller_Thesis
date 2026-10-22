using RimTelligence.CustomEvents.Handlers;
using RimTelligence.DataSnapshots;

namespace RimTelligence.CustomEvents.EventSelector.Modifiers
{
    public class RaidModifiers : IEventModifier
    {
        public float Modify(IEffect entry, ColonySnapshot snapshot)
        {
            float multiplier = 1f;

            if (snapshot.daysSinceLastRaid > 20)
            {
                if (entry.Config.EffectName == "AnimalInsanity") multiplier *= 2.0f;
                if (entry.Config.EffectName == "StartExtremeWeather") multiplier *= 1.3f;
                if (entry.Config.EffectName == "DamagePawn") multiplier *= 2.5f;
            }
            if (snapshot.daysSinceLastRaid <= 2 && snapshot.daysSinceLastRaid >= 0)
            {
                // Fresh raid — lean toward aftermath events
                if (entry.Config.EffectName == "MiracleHealDisease") multiplier *= 1.4f;
                if (entry.Config.EffectName == "MiracleHealMissingPart") multiplier *= 1.01f;
                if (entry.Config.EffectName == "AddExpPoints") multiplier *= 1.9f;
                if (entry.Config.EffectName == "MoodDebuff") multiplier *= 2f;
                // Avoid another immediate assault-type event
                if (entry.Config.EffectName == "DamagePawn") multiplier *= 0.4f;
                if (entry.Config.EffectName == "AnimalInsanity") multiplier *= 0.4f;
            }
            return multiplier;
        }
    }
}