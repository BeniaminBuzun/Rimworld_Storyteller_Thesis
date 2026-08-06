using RimTelligence.DataSnapshots;

namespace RimTelligence.CustomEvents.EventSelector.Modifiers
{
    public interface IEventModifier
    {
        float Modify(EventEntry entry, ColonySnapshot snapshot);
    }
}