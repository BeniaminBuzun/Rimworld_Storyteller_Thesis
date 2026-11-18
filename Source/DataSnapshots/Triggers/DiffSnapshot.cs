using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;

namespace RimTelligence.Source.DataSnapshots.Triggers
{
    internal class DiffSnapshot
    {
        private static HashSet<int> foundThings = new HashSet<int>();
        public void RunBefore(Map map)
        {
            foreach (Thing t in map.listerThings.AllThings) foundThings.Add(t.thingIDNumber);

        }
        public List<Thing> RunAfter(Map map)
        {
            var result = new List<Thing>();

            foreach (Thing t in map.listerThings.AllThings)
            {
                if (!foundThings.Contains(t.thingIDNumber))
                    result.Add(t);
                {
                }
            }
            return result;
        }
    }
}
