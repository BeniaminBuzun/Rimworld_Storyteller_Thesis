using RimTelligence.CustomEvents;
using RimTelligence.CustomEvents.Handlers;
using RimTelligence.CustomEvents.Handlers.Pawns.Debuffs;
using RimTelligence.DataSnapshots;
using RimWorld;
using RimWorld.Planet;
using System;
using System.Linq;
using Verse;

namespace RimTelligence.Source.CustomEvents.Handlers.Pawns.Debuffs
{
    internal class MoodDebuff : IEffect
    {
        public EffectConfig Config => EffectConfigRegistry.Get("MoodDebuff");

        public bool CanBeChoosen(ColonySnapshot snapshot) => true;

        public GlobalTargetInfo Execute(GameEffect effect, Map map)
        {
            Pawn target = map.mapPawns.FreeColonistsSpawned
                .FirstOrDefault(pawn => string.Equals(pawn.LabelShort, effect.TargetPawn, StringComparison.OrdinalIgnoreCase));

            if (target == null)
            {
                ModLogger.Warning($"MoodDebuff: no target found for '{effect.TargetPawn}'.");
                return GlobalTargetInfo.Invalid;
            }

            string[] defNames = effect.SelectedDefName ?? new string[0];
            string label = defNames.Length > 0 ? defNames[0] : "Unusual feeling";
            string description = defNames.Length > 1 ? defNames[1] : label;
            string uniqueKey = $"RimTelligence_MoodDebuff_{(defNames.Length > 0 ? Math.Abs(defNames[0].GetHashCode()) : 0)}_{System.Environment.TickCount & 0xFFFF}";
            int moodOffset = -MoodHelper.CalculateMoodOffset(effect.Severity);
            ThoughtDef def = MoodHelper.GetOrCreateThoughtDef(uniqueKey, label, description, moodOffset);

            var memory = (ThoughtMemoryDynamic)ThoughtMaker.MakeThought(def);
            memory.customLabel = label;
            memory.customDescription = description;

            target.needs?.mood?.thoughts?.memories?.TryGainMemory(memory);
            ModLogger.Message($"MoodDebuff: applied '{label}' ({moodOffset:+#;-#}) to {target.LabelShort}.");

            return new GlobalTargetInfo(target);
        }

        public string GetSummary(GameEffect effect)
        {
            string label = effect.SelectedDefName != null && effect.SelectedDefName.Length > 0
                ? effect.SelectedDefName[0]
                : "Unusual feeling";
            return $"Mood debuff '{label}' applied to {effect.TargetPawn}";
        }
    }
}
