using RimTelligence.DataSnapshots;
using RimWorld;
using RimWorld.Planet;
using System;
using System.Collections.Generic;
using System.Linq;
using Verse;

namespace RimTelligence.CustomEvents.Handlers.Item
{
    public class LoseItems : IEffect
    {
        private readonly EffectConfig _config = EffectConfigRegistry.Get("LoseItems");

        public EffectConfig Config => _config;

        public bool CanBeChoosen(ColonySnapshot snapshot) => snapshot?.colonyResources?.Count > 0;

        private string executionSummary;

        public GlobalTargetInfo Execute(GameEffect effect, Map map)
        {
            if (effect.SelectedDefName == null || effect.SelectedDefName.Length == 0 ||
                string.IsNullOrWhiteSpace(effect.SelectedDefName[0]))
            {
                executionSummary = "An inventory discrepancy occurred, but no specific items were lost.";
                return GlobalTargetInfo.Invalid;
            }

            ThingDef def = DefDatabase<ThingDef>.GetNamed(effect.SelectedDefName[0], errorOnFail: false);
            if (def == null)
            {
                ModLogger.Message($"{Config.EffectName}: unrecognized item type '{effect.DefNameText}'; nothing was removed.");
                executionSummary = $"An inventory audit showed missing supplies, but {effect.DefNameText} could not be found.";
                return GlobalTargetInfo.Invalid;
            }

            string itemLabel = def.LabelCap.Resolve();
            int toRemove = ItemHandlerSupport.ResolveQuantity(def, effect.Severity, map);
            int leftToLose = toRemove;

            List<Thing> stored = map.listerThings.ThingsOfDef(def)
                .Where(thing => thing.Spawned && !thing.Destroyed && thing.GetSlotGroup() != null)
                .InRandomOrder()
                .ToList();

            foreach (Thing thing in stored)
            {
                if (leftToLose <= 0) break;
                int take = Math.Min(thing.stackCount, leftToLose);
                thing.SplitOff(take).Destroy(DestroyMode.Vanish);
                leftToLose -= take;
            }

            int removed = toRemove - leftToLose;

            ModLogger.Message($"{Config.EffectName}: removed {removed}x {effect.DefNameText} " +
                              $"(value={effect.Severity}, requested={toRemove})");

            if (removed > 0)
            {
                executionSummary = $"Stockpile loss: {removed}x {itemLabel} were ruined or went missing from storage!";
            }
            else
            {
                executionSummary = $"A storage mishap threatened supplies, but no {itemLabel} were found in the stockpiles to lose.";
            }

            return GlobalTargetInfo.Invalid;
        }

        public string GetSummary(GameEffect effect)
        {
            if (!string.IsNullOrEmpty(executionSummary))
                return executionSummary;

            return GetDefaultSummary(effect);
        }

        private static string GetDefaultSummary(GameEffect effect)
        {
            string itemLabel = ResolveItemLabel(effect);
            string sevStr = $" ({effect.Severity.ToString().ToLower()})";
            return $"Stockpile loss: supplies of {itemLabel} are spoiled or destroyed{sevStr}.";
        }

        private static string ResolveItemLabel(GameEffect effect)
        {
            string defName = effect.SelectedDefName != null && effect.SelectedDefName.Length > 0 ? effect.SelectedDefName[0] : effect.DefNameText;
            if (string.IsNullOrWhiteSpace(defName)) return "supplies";
            ThingDef def = DefDatabase<ThingDef>.GetNamed(defName, errorOnFail: false);
            return def != null ? def.LabelCap.Resolve() : defName;
        }
    }
}