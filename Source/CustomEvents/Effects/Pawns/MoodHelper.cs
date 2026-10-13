using RimWorld;
using System;
using System.Collections.Generic;
using Verse;

namespace RimTelligence.CustomEvents.Handlers.Pawns.Debuffs
{
    internal class MoodHelper
    {
        private static readonly Random _random = new Random();

        public static ThoughtDef GetOrCreateThoughtDef(string defName, string label, string description, float moodOffset)
        {
            ThoughtDef existing = DefDatabase<ThoughtDef>.GetNamed(defName, errorOnFail: false);
            if (existing != null) return existing;

            ThoughtDef def = new ThoughtDef
            {
                defName = defName,
                thoughtClass = typeof(ThoughtMemoryDynamic),
                durationDays = 1f,
                stackedEffectMultiplier = 1f,
                stages = new List<ThoughtStage>
                {
                    new ThoughtStage
                    {
                        label          = label ?? defName,
                        description    = description ?? string.Empty,
                        baseMoodEffect = moodOffset,
                    }
                }
            };

            DefDatabase<ThoughtDef>.Add(def);
            return def;
        }
        public static int CalculateMoodOffset(SeverityTypes severity)
        {
            {
                switch (severity)
                {
                    case SeverityTypes.Low: return _random.Next(2, 4);
                    case SeverityTypes.Medium: return _random.Next(4, 7);
                    case SeverityTypes.High: return _random.Next(7, 12);
                    case SeverityTypes.Extreme: return _random.Next(15, 21);
                    default: return 0;
                }
            }
        }
    }


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
