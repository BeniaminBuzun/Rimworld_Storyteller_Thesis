using RimWorld;
using RimWorld.Planet;
using System.Collections.Generic;
using Verse;

namespace RimTelligence.CustomEvents.Workers
{
    public class ChoiceLetter_Procedural : ChoiceLetter
    {
        public List<string> jumpLabels = new List<string>();
        public List<GlobalTargetInfo> jumpTargets = new List<GlobalTargetInfo>();

        public override IEnumerable<DiaOption> Choices
        {
            get
            {
                for (int i = 0; i < jumpTargets.Count; i++)
                {
                    GlobalTargetInfo target = jumpTargets[i]; // capture per iteration
                    if (!target.IsValid) continue;

                    yield return new DiaOption("Jump to: " + jumpLabels[i])
                    {
                        action = () => CameraJumper.TryJumpAndSelect(target),
                        resolveTree = false // keep the letter open
                    };
                }

                yield return Option_Close;
            }
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Collections.Look(ref jumpLabels, "jumpLabels", LookMode.Value);
            Scribe_Collections.Look(ref jumpTargets, "jumpTargets", LookMode.GlobalTargetInfo);

            if (Scribe.mode == LoadSaveMode.PostLoadInit)
            {
                if (jumpLabels == null) jumpLabels = new List<string>();
                if (jumpTargets == null) jumpTargets = new List<GlobalTargetInfo>();
            }
        }
    }
}