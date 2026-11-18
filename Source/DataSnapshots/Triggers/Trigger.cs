using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;

namespace RimTelligence.Source.DataSnapshots.Triggers
{
    public abstract class Trigger
    {
        public virtual string Name { get; }
        public virtual string Description { get; }

        public abstract void RunBefore(Map map);
        public abstract bool RunAfter(Map map);

        public override string ToString()
        {
            return $"{Name}, effect: {Description}";
        }

    }
}
