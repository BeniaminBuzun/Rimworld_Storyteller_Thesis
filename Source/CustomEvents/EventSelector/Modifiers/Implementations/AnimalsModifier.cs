using RimTelligence.Source.DataSnapshots;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RimTelligence.Source.CustomEvents.EventSelector.Modifiers.Implementations
{
    public class AnimalsModifier : IEventModifier
    {

        public bool ShouldApply(ColonySnapshot snapshot) => true;

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
