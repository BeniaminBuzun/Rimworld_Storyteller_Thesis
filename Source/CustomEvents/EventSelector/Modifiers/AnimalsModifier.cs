using RimTelligence.DataSnapshots;

namespace RimTelligence.CustomEvents.EventSelector.Modifiers
{
    public class AnimalsModifier : IEventModifier
    {

        public float Modify(EventEntry entry, ColonySnapshot snapshot)
        {
            float multiplier = 1f;

            if (snapshot.colonyAnimalCount >= 10)
            {
                if (entry.Type == "SelfTame") multiplier *= 0.8f;
                if (entry.Type == "AnimalInsanity") multiplier *= 1.5f;
            }
            if (snapshot.colonyAnimalCount < 5)
            {
                if (entry.Type == "SelfTame") multiplier *= 2f;
            }
            return multiplier;

        }

    }
}