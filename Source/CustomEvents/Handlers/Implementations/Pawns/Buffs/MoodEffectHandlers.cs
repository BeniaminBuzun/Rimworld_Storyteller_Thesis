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
        public abstract string EffectName { get; }

        public abstract string NarrativeDescription { get; }
        public abstract string PromptStructure { get; }
        public abstract string AditionalPromptStructureInfo { get; }
        protected abstract float MoodOffset { get; }

        public abstract float BaseWeight { get; }

        public abstract EventAlignment Alignment { get; }

        public GlobalTargetInfo Execute(GameEffect effect, Map map)
        {
            Pawn target = map.mapPawns.FreeColonistsSpawned
                .FirstOrDefault(p => string.Equals(p.LabelShort, effect.targetPawn, StringComparison.OrdinalIgnoreCase));

            if (target == null)
            {
                ModLogger.Warning($"{EffectName}: no target found for '{effect.targetPawn}'.");
                return GlobalTargetInfo.Invalid;
            }

            string uniqueKey = $"RimTelligence_{EffectName}_{Math.Abs(effect.defName?.GetHashCode() ?? 0)}_{Environment.TickCount & 0xFFFF}";
            ThoughtDef def = GetOrCreateThoughtDef(uniqueKey, effect.defName, effect.defDescription, MoodOffset);

            var memory = (ThoughtMemoryDynamic)ThoughtMaker.MakeThought(def);
            memory.customLabel = effect.defName;
            memory.customDescription = effect.defDescription;

            target.needs?.mood?.thoughts?.memories?.TryGainMemory(memory);
            ModLogger.Message($"{EffectName}: applied '{effect.defName}' ({MoodOffset:+#;-#}) to {target.LabelShort}.");

            return new GlobalTargetInfo(target);
        }

        public abstract string GetSummary(GameEffect effect);

        private static ThoughtDef GetOrCreateThoughtDef(string defName, string label, string description, float moodOffset)
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
    }

    public class MoodBuffHandler : MoodEffectHandlerBase
    {
        public override string EffectName => "MoodBuff";

        public override string NarrativeDescription => "An event where a specific colonist receives a positive mood effect.";

        public override string PromptStructure =>
            "- type = MoodBuff:\r\n" +
            "    defName = \"thought title\" (short label shown in mood breakdown)\r\n" +
            "    count = 0\r\n" +
            "    defDescription = \"flavour text describing why the colonist feels good\"\r\n" +
            "    value = NONE\r\n" +
            "    targetPawn = \"colonist name\"";

        public override string AditionalPromptStructureInfo =>
            "";

        public override float BaseWeight => 6f;

        public override EventAlignment Alignment => EventAlignment.Good;

        protected override float MoodOffset => 5f;

        public override string GetSummary(GameEffect effect) =>
            $"Mood buff '{effect.defName}' (+5) applied to {effect.targetPawn}";
    }

    public class MoodDebuffHandler : MoodEffectHandlerBase
    {
        public override string EffectName => "MoodDebuff";

        public override string NarrativeDescription => "An event where a specific colonist receives a negative mood effect.";


        public override string PromptStructure =>
            "- type = MoodDebuff:\r\n" +
            "    defName = \"thought title\" (short label shown in mood breakdown)\r\n" +
            "    count = 0\r\n" +
            "    defDescription = \"flavour text describing why the colonist feels bad\"\r\n" +
            "    value = NONE\r\n" +
            "    targetPawn = \"colonist name\"";

        public override string AditionalPromptStructureInfo =>
            "";

        public override float BaseWeight => 5f;

        public override EventAlignment Alignment => EventAlignment.Bad;

        protected override float MoodOffset => -5f;

        public override string GetSummary(GameEffect effect) =>
            $"Mood debuff '{effect.defName}' (-5) applied to {effect.targetPawn}";
    }
}
