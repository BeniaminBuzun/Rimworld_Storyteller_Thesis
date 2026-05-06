using RimTelligence.Source.CustomEvents.Handlers.Implementations.Pawns.Debuffs;
using RimWorld;
using RimWorld.Planet;
using System;
using System.Collections.Generic;
using System.Linq;
using Verse;

namespace RimTelligence.Source.CustomEvents.Handlers
{
    public abstract class MoodEffectHandlerBase : IEffectHandler
    {
        public abstract string EffectType { get; }
        public abstract string PromptDescription { get; }
        public abstract string AditionalPromptInfo { get; }
        protected abstract float MoodOffset { get; }

        public GlobalTargetInfo Execute(LlmEffect effect, Map map)
        {
            Pawn target = map.mapPawns.FreeColonistsSpawned
                .FirstOrDefault(p => string.Equals(p.LabelShort, effect.targetPawn, StringComparison.OrdinalIgnoreCase));

            if (target == null)
            {
                ModLogger.Warning($"{EffectType}: No target found for '{effect.targetPawn}'.");
                return GlobalTargetInfo.Invalid;
            }

            string uniqueKey = $"RimTelligence_{EffectType}_{Math.Abs(effect.defName?.GetHashCode() ?? 0)}_{Environment.TickCount & 0xFFFF}";
            ThoughtDef def = GetOrCreateThoughtDef(uniqueKey, effect.defName, effect.defDescription, MoodOffset);

            var memory = (ThoughtMemoryDynamic)ThoughtMaker.MakeThought(def);
            memory.customLabel = effect.defName;
            memory.customDescription = effect.defDescription;

            target.needs?.mood?.thoughts?.memories?.TryGainMemory(memory);
            ModLogger.Message($"{EffectType}: Applied '{effect.defName}' ({MoodOffset:+#;-#}) to {target.LabelShort}.");

            return new GlobalTargetInfo(target);
        }

        public abstract string GetSummary(LlmEffect effect);

        private static ThoughtDef GetOrCreateThoughtDef(string defName, string label, string description, float moodOffset)
        {
            ThoughtDef existing = DefDatabase<ThoughtDef>.GetNamed(defName, errorOnFail: false);
            if (existing != null)
                return existing;

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
    }

    public class MoodBuffHandler : MoodEffectHandlerBase
    {
        public override string EffectType => "MoodBuff";
        public override string PromptDescription => "- MoodBuff   : defName = thought title, defDescription = flavour text, targetPawn = pawn name";
        public override string AditionalPromptInfo => "Applies a +5 mood buff with a custom title and description. Can stack — each call creates a separate memory.";
        protected override float MoodOffset => 5f;

        public override string GetSummary(LlmEffect effect) =>
            $"Mood buff '{effect.defName}' (+5) applied to {effect.targetPawn}";
    }

    public class MoodDebuffHandler : MoodEffectHandlerBase
    {
        public override string EffectType => "MoodDebuff";
        public override string PromptDescription => "- MoodDebuff : defName = thought title, defDescription = flavour text, targetPawn = pawn name";
        public override string AditionalPromptInfo => "Applies a -5 mood debuff with a custom title and description. Can stack — each call creates a separate memory.";
        protected override float MoodOffset => -5f;

        public override string GetSummary(LlmEffect effect) =>
            $"Mood debuff '{effect.defName}' (-5) applied to {effect.targetPawn}";
    }
}