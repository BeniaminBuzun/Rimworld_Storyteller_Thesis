using RimTelligence.DataSnapshots;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RimTelligence.CustomEvents.EventSelector.Modifiers
{
    public interface IEventModifier
    {
        bool ShouldApply(ColonySnapshot snapshot);

        float Modify(EventEntry entry, ColonySnapshot snapshot);
    }
}