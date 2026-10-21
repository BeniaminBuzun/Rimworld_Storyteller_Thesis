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

        private string executionSummary;

        public GlobalTargetInfo Execute(GameEffect effect, Map map)
        {
            var colonists = map.mapPawns.FreeColonistsSpawned.ToList();
            Pawn target = null;
            if (!string.IsNullOrWhiteSpace(effect.TargetPawn) && !effect.TargetPawn.Equals("random", StringComparison.OrdinalIgnoreCase))
            {
                target = colonists.FirstOrDefault(pawn =>
                    string.Equals(pawn.LabelShort, effect.TargetPawn, StringComparison.OrdinalIgnoreCase));
            }

            if (target == null)
            {
                target = colonists.RandomElementWithFallback();
            }

            if (target == null)
            {
                ModLogger.Warning($"MoodDebuff: no target found for '{effect.TargetPawn}'.");
                executionSummary = "A wave of gloom swept over the colony, but found no mind to torment.";
                return GlobalTargetInfo.Invalid;
            }

            string[] defNames = effect.SelectedDefName ?? new string[0];
            string label = defNames.Length > 0 ? defNames[0] : "Troubled mind";
            string description = defNames.Length > 1 ? defNames[1] : label;
            int moodOffset = -MoodHelper.CalculateMoodOffset(effect.Severity);
            ThoughtDef def = DefDatabase<ThoughtDef>.GetNamedSilentFail("RimTelligence_DynamicMoodDebuff")
                             ?? MoodHelper.EnsureLegacyThoughtDefExists("RimTelligence_DynamicMoodDebuff");

            var memory = (ThoughtMemoryDynamic)ThoughtMaker.MakeThought(def);
            memory.customLabel = label;
            memory.customDescription = description;
            memory.SetCustomMoodOffset(moodOffset);

            target.needs?.mood?.thoughts?.memories?.TryGainMemory(memory);

            executionSummary = !string.IsNullOrWhiteSpace(description) && !string.Equals(description, label, StringComparison.OrdinalIgnoreCase)
                ? $"{target.LabelShort} is distressed ({moodOffset} mood): \"{description}\""
                : $"{target.LabelShort} is distressed: '{label}' ({moodOffset} mood).";

            ModLogger.Message($"MoodDebuff: applied '{label}' ({moodOffset:+#;-#}) to {target.LabelShort}.");

            return new GlobalTargetInfo(target);
        }

        public string GetSummary(GameEffect effect)
        {
            if (!string.IsNullOrEmpty(executionSummary))
                return executionSummary;

            return GetDefaultSummary(effect);
        }

        private static string GetDefaultSummary(GameEffect effect)
        {
            string label = effect.SelectedDefName != null && effect.SelectedDefName.Length > 0
                ? effect.SelectedDefName[0]
                : (!string.IsNullOrWhiteSpace(effect.DefNameText) ? effect.DefNameText : "Troubled mind");
            string pawn = !string.IsNullOrWhiteSpace(effect.TargetPawn) ? effect.TargetPawn : "A colonist";
            string sevStr = effect.Severity != SeverityTypes.None ? $" ({effect.Severity.ToString().ToLower()})" : "";
            return $"{pawn} suffers a distressing mood penalty: '{label}'{sevStr}.";
        }
    }
}
