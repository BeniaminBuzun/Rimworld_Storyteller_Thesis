using RimTelligence.DataSnapshots;
using System;

namespace RimTelligence.CustomEvents.EventSelector.Modifiers
{
    public class FoodModifier : IEventModifier
    {
        public float Modify(EventEntry entry, ColonySnapshot snapshot)
        {
            float multiplier = 1f;

            float daysLeft = snapshot.totalNutrition / Math.Max(snapshot.colonistCount * 1.6f, 1f);

            if (daysLeft < 3f)
            {
                if (entry.Type.StartsWith("ResourcePodCrash") && entry.Type.Contains("Food")) multiplier *= 2.0f;
                if (entry.Type.StartsWith("PawnFoundItem") && entry.Type.Contains("Food")) multiplier *= 1.5f;
                if (entry.Type.StartsWith("LoseItems") && entry.Type.Contains("Food")) multiplier *= 0.4f;
            }
            else if (daysLeft > 10f)
            {
                if (entry.Type.Contains("Food")) multiplier *= 0.2f;
                if (entry.Type.StartsWith("LoseItems") && entry.Type.Contains("Food")) multiplier *= 2f;
            }
            return multiplier;
        }
    }
}