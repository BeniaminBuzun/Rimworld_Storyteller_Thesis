using RimTelligence.DataSnapshots;
using RimWorld;
using RimWorld.Planet;
using System;
using System.Collections.Generic;
using Verse;

namespace RimTelligence.CustomEvents.Handlers.Item
{
    public class ResourcePodCrash : IEffect
    {
        private readonly EffectConfig _config = EffectConfigRegistry.Get("ResourcePodCrash");

        public EffectConfig Config => _config;

        public bool CanBeChoosen(ColonySnapshot snapshot) => true;

        private string executionSummary;

        public GlobalTargetInfo Execute(GameEffect effect, Map map)
        {
            ThingDef def = ResolveItemDef(effect);
            if (def == null || !ItemHandlerSupport.IsSpawnable(def))
            {
                ModLogger.Warning($"ResourcePodCrash: '{effect.DefNameText}' is missing or not spawnable.");
                executionSummary = "Debris from orbit burnt up high in the atmosphere before reaching the surface.";
                return GlobalTargetInfo.Invalid;
            }

            int quantity = ItemHandlerSupport.ResolveQuantity(def, effect.Severity, map);
            int pods = Rand.RangeInclusive(1, 4);
            var items = SplitIntoStacks(def, quantity, pods);
            pods = Math.Min(pods, items.Count);

            IntVec3 cell = DropCellFinder.RandomDropSpot(map);
            DropPodUtility.DropThingsNear(cell, map, items);

            string itemLabel = def.LabelCap.Resolve();
            executionSummary = $"Cargo pods crashed down from orbit, scattering {quantity}x {itemLabel} across {pods} container{(pods == 1 ? "" : "s")}!";

            ModLogger.Message($"{Config.EffectName}: dropped {quantity}x {def.defName} " +
                              $"(value={effect.Severity}, pods={pods})");

            return new GlobalTargetInfo(cell, map);
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
            return $"Cargo pods carrying {itemLabel} hurtle down from orbit{sevStr}.";
        }

        private static ThingDef ResolveItemDef(GameEffect effect)
        {
            string defName = effect.SelectedDefName != null && effect.SelectedDefName.Length > 0 ? effect.SelectedDefName[0] : effect.DefNameText;
            if (string.IsNullOrWhiteSpace(defName)) return null;
            return DefDatabase<ThingDef>.GetNamed(defName, errorOnFail: false);
        }

        private static string ResolveItemLabel(GameEffect effect)
        {
            ThingDef def = ResolveItemDef(effect);
            return def != null ? def.LabelCap.Resolve() : (!string.IsNullOrWhiteSpace(effect.DefNameText) ? effect.DefNameText : "supplies");
        }

        private static List<Thing> SplitIntoStacks(ThingDef def, int total, int pods)
        {
            var items = new List<Thing>();
            int baseCount = total / pods;
            int remainder = total % pods;

            for (int index = 0; index < pods; index++)
            {
                int left = baseCount + (index < remainder ? 1 : 0);
                while (left > 0)
                {
                    Thing thing = ItemHandlerSupport.MakeItem(def, left);
                    items.Add(thing);
                    left -= thing.stackCount;
                }
            }
            return items;
        }
    }
}
