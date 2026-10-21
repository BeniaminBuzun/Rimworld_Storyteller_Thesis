using RimWorld;
using System;
using System.Collections.Generic;
using Verse;

namespace RimTelligence.CustomEvents.Handlers.Pawns.Debuffs
{
    internal class MoodHelper
    {
        private static readonly Random _random = new Random();
        private static readonly HashSet<string> RegisteredLegacyDefs = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        public static ThoughtDef EnsureLegacyThoughtDefExists(string defName)
        {
            if (string.IsNullOrEmpty(defName)) return null;

            lock (RegisteredLegacyDefs)
            {
                if (RegisteredLegacyDefs.Contains(defName))
                {
                    return DefDatabase<ThoughtDef>.GetNamedSilentFail(defName);
                }

                ThoughtDef existing = DefDatabase<ThoughtDef>.GetNamedSilentFail(defName);
                if (existing != null)
                {
                    RegisteredLegacyDefs.Add(defName);
                    return existing;
                }

                bool isBuff = defName.StartsWith("RimTelligence_MoodBuff_");
                ThoughtDef def = new ThoughtDef
                {
                    defName = defName,
                    thoughtClass = typeof(ThoughtMemoryDynamic),
                    durationDays = 1f,
                    stackedEffectMultiplier = 1f,
                    stackLimit = 10,
                    stages = new List<ThoughtStage>
                    {
                        new ThoughtStage
                        {
                            label          = isBuff ? "Positive Event" : "Negative Event",
                            description    = isBuff ? "Something good happened." : "Something bad happened.",
                            baseMoodEffect = isBuff ? 5 : -5,
                        }
                    }
                };

                RegisteredLegacyDefs.Add(defName);
                DefDatabase<ThoughtDef>.Add(def);
                ModLogger.Message($"[MoodHelper] Registered dynamic legacy ThoughtDef on the fly: {defName}");
                return def;
            }
        }

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
                stackLimit = 10,
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


    public class ThoughtMemoryDynamic : Thought_Memory
    {
        public string customLabel;
        public string customDescription;
        public float customMoodOffset;
        public bool hasCustomMoodOffset;

        public override string LabelCap => !string.IsNullOrEmpty(customLabel) ? customLabel.CapitalizeFirst() : base.LabelCap;
        public override string Description => !string.IsNullOrEmpty(customDescription) ? customDescription : base.Description;

        public override float MoodOffset()
        {
            if (hasCustomMoodOffset)
            {
                return customMoodOffset;
            }
            return base.MoodOffset();
        }

        public void SetCustomMoodOffset(float offset)
        {
            customMoodOffset = offset;
            hasCustomMoodOffset = true;
        }

        public override void ExposeData()
        {
            if (Scribe.mode == LoadSaveMode.LoadingVars)
            {
                try
                {
                    string legacyDefName = Scribe.loader?.curXmlParent?["def"]?.InnerText;
                    if (!string.IsNullOrEmpty(legacyDefName) &&
                        (legacyDefName.StartsWith("RimTelligence_MoodBuff_") || legacyDefName.StartsWith("RimTelligence_MoodDebuff_")))
                    {
                        MoodHelper.EnsureLegacyThoughtDefExists(legacyDefName);
                    }
                }
                catch (Exception ex)
                {
                    ModLogger.Warning($"Could not pre-resolve legacy ThoughtDef: {ex.Message}");
                }
            }

            base.ExposeData();
            Scribe_Values.Look(ref customLabel, "customLabel");
            Scribe_Values.Look(ref customDescription, "customDescription");
            Scribe_Values.Look(ref customMoodOffset, "customMoodOffset", 0f);
            Scribe_Values.Look(ref hasCustomMoodOffset, "hasCustomMoodOffset", false);
        }
    }

}
