using RimTelligence.CustomEvents.Handlers;
using RimTelligence.DataSnapshots;

namespace RimTelligence.CustomEvents.EventSelector.Modifiers
{
    public interface IEventModifier
    {
        float Modify(IEffect entry, ColonySnapshot snapshot);
    }
}