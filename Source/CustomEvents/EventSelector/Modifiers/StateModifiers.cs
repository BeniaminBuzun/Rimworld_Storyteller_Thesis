using RimTelligence.DataSnapshots;
using System.Collections.Generic;

namespace RimTelligence.CustomEvents.EventSelector.Modifiers
{
    public static class StateModifiers
    {
        private static readonly IReadOnlyList<IEventModifier> _modifiers = new List<IEventModifier>
        {
            new MoodModifier(),
            new FoodModifier(),
            new AnimalsModifier(),
            new HasActiveConditionModifier(),
            new SeasonWinterModifier(),
            new DawnedColonistsModifier(),
            new RaidModifiers(),
        };
        public static float Apply(EventEntry entry, ColonySnapshot snapshot)
        {
            float multiplier = 1f;

            foreach (var modifier in _modifiers)
            {    
                multiplier *= modifier.Modify(entry, snapshot);
            }

            return multiplier;
        }
    }
}