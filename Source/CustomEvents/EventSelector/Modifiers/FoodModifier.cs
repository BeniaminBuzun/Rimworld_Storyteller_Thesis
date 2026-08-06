using RimTelligence.DataSnapshots;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RimTelligence.CustomEvents.EventSelector.Modifiers
{
    public class FoodModifier : IEventModifier
    {
        public bool ShouldApply(ColonySnapshot snapshot) => true;

        public float Modify(EventEntry entry, ColonySnapshot snapshot)
        {
            float multiplier = 1f;

            float daysLeft = snapshot.totalNutrition / Math.Max(snapshot.colonistCount * 1.6f, 1f);

            if (daysLeft < 3f)
            {
                if (entry.Family == "ResourcePodCrash" && entry.Type.Contains("Food")) multiplier *= 2.0f;
                if (entry.Family == "PawnFoundItem" && entry.Type.Contains("Food")) multiplier *= 1.5f;
                if (entry.Family == "LoseItems" && entry.Type.Contains("Food")) multiplier *= 0.4f;
            }
            else if (daysLeft > 10f)
            {
                if (entry.Type.Contains("Food")) multiplier *= 0.2f;
                if (entry.Family == "LoseItems" && entry.Type.Contains("Food")) multiplier *= 2f;
            }
            return multiplier;
        }
    }
}