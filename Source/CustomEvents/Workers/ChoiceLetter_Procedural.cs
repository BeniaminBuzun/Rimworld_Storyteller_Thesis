using RimTelligence.CustomEvents.Handlers;
using RimTelligence.Source.CustomEvents.Handlers;
using RimWorld;
using RimWorld.Planet;
using System;
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
        public static LetterDef Resolve(LlmCustomEvent evt)
        {
            if (evt?.effects == null || evt.effects.Count == 0)
                return LetterDefOf.NeutralEvent;

            bool anyGood = false, anyBad = false;

            foreach (GameEffect effect in evt.effects)
            {
                if (effect == null || string.IsNullOrWhiteSpace(effect.EffectName))
                    continue;

                IEffect handler;
                try { handler = EffectFactory.Create(effect.EffectName); }
                catch (Exception) { continue; }   // missing config: ignore this effect for styling

                if (handler?.Config == null)
                    continue;

                switch (handler.Config.Alignment)
                {
                    case EventAlignment.Good: anyGood = true; break;
                    case EventAlignment.Bad: anyBad = true; break;
                }
            }

            if (anyGood && !anyBad) return LetterDefOf.PositiveEvent;
            if (anyBad) return LetterDefOf.NegativeEvent;
            return LetterDefOf.NeutralEvent;
        }
    }
}