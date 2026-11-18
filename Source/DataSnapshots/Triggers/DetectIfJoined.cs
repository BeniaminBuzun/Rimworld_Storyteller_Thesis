using RimTelligence.Source.DataSnapshots.Triggers;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;

namespace RimTelligence.Source.DataSnapshots.Triggers
{
    internal class DetectIfJoined : Trigger
    {
        public override string Name => "Detect if joined";
        public override string Description => "Runs next event when pawn joins the colony";

        private DiffSnapshot snapshot;
        public override void RunBefore(Map map)
        {
            snapshot = new DiffSnapshot();
            snapshot.RunBefore(map);
        }

        public override bool RunAfter(Map map)
        {
            List<Thing> things = snapshot.RunAfter(map);

            foreach (Thing t in things)
            {
                if (t is Pawn)
                {
                    if (t.Faction == Faction.OfPlayer)
                    {
                        return true;
                    }
                }
            }
            return false;
        }

    }
}
