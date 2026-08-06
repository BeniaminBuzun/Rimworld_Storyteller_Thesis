using RimTelligence.DataSnapshots;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace RimTelligence.CustomEvents.EventSelector.Modifiers
{

    public class MoodModifier : IEventModifier
    {
        public bool ShouldApply(ColonySnapshot snapshot) => snapshot.colonists?.Count > 0;

        public float Modify(EventEntry entry, ColonySnapshot snapshot)
        {
            float avgMood = snapshot.colonists.Average(pawn => pawn.needs?.mood ?? 0.5f);
            float multiplier = 1f;

            if (avgMood < 0.25f)
            {
                if (entry.Type == "MoodBuff") multiplier *= 2.5f;
                if (entry.Type == "MoodDebuff") multiplier *= 0.3f;
            }
            if (avgMood > 0.75f)
            {
                if (entry.Type == "MoodDebuff") multiplier *= 1.5f;
                if (entry.Type == "MoodBuff") multiplier *= 0.6f;
            }
            return multiplier;
        }
    }
}