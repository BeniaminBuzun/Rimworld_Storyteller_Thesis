using RimTelligence.CustomEvents.Handlers;
using RimTelligence.DataSnapshots;
using System.Collections.Generic;

namespace RimTelligence.CustomEvents.EventSelector.Modifiers
{
    public static class StateModifiers
    {
        private static readonly IReadOnlyList<IEventModifier> _modifiers = new List<IEventModifier>
        {
            new MoodModifier(),
            new AnimalsModifier(),
            new HasActiveConditionModifier(),
            new SeasonWinterModifier(),
            new DownedColonistsModifier(),
            new RaidModifiers(),
        };
        public static float Apply(IEffect entry, ColonySnapshot snapshot)
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