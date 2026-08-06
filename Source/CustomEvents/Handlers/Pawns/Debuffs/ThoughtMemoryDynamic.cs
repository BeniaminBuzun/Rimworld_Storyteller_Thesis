using RimWorld;
using Verse;

namespace RimTelligence.CustomEvents.Handlers.Pawns.Debuffs
{
    public class ThoughtMemoryDynamic : Thought_Memory
    {
        public string customLabel;
        public string customDescription;

        public override string LabelCap => customLabel != null ? customLabel.CapitalizeFirst() : base.LabelCap;
        public override string Description => customDescription ?? base.Description;

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref customLabel, "customLabel");
            Scribe_Values.Look(ref customDescription, "customDescription");
        }
    }
}