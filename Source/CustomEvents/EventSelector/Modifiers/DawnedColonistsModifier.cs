using RimTelligence.DataSnapshots;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;

namespace RimTelligence.CustomEvents.EventSelector.Modifiers
{
    public class DawnedColonistsModifier : IEventModifier
    {
        public bool ShouldApply(ColonySnapshot snapshot) => snapshot.colonists?.Any(colonist => colonist.health?.is_downed == true) ?? false;

        public float Modify(EventEntry entry, ColonySnapshot snapshot)
        {
            float multiplier = 1f;
            
            if (entry.Type == "MiracleHealChronicCondition") multiplier *= 1.3f;
            if (entry.Type == "MiracleHealMissingPart") multiplier *= 1.5f;
            if (entry.Type == "DiseasePawn") multiplier *= 0.5f;
            if (entry.Type == "DamagePawn") multiplier *= 0.3f;
            if (entry.Type == "MoodBuff") multiplier = 2f;
            if (entry.Type == "MoodDebuff") multiplier = 2f;

            return multiplier;
        }
    }
}