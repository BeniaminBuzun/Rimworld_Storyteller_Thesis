using RimTelligence.Source.DataSnapshots;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;

namespace RimTelligence.Source.CustomEvents.EventSelector.Modifiers.Implementations
{
    public class RaidModifiers : IEventModifier
    {
        public bool ShouldApply(ColonySnapshot snapshot) => true;
        public float Modify(EventEntry entry, ColonySnapshot snapshot)
        {
            float multiplier = 1f;

            if (snapshot.daysSinceLastRaid > 20)
            {
                if (entry.Type == "AnimalInsanity") multiplier *= 2.0f;
                if (entry.Type == "StartExtremeWeatherEvent") multiplier *= 1.3f;
                if (entry.Type == "DamagePawn") multiplier *= 2.5f;
            }
            if (snapshot.daysSinceLastRaid <= 2)
            {
                // Fresh raid — lean toward aftermath events
                if (entry.Type == "MiracleHealDisease") multiplier *= 1.4f;
                if (entry.Type == "MiracleHealMissingPart") multiplier *= 1.01f;
                if (entry.Type == "AddExpPoints") multiplier *= 1.9f;
                if (entry.Type == "MoodDebuff") multiplier *= 2f;
                // Avoid another immediate assault-type event
                if (entry.Type == "DamagePawn") multiplier *= 0.4f;
                if (entry.Type == "AnimalInsanity") multiplier *= 0.4f;
            }
            return multiplier;
        }
    }
}
