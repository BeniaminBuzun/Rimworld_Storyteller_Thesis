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
        public EffectConfig Config => EffectConfigRegistry.Get("ResourcePodCrash");

        public bool CanBeChoosen(ColonySnapshot snapshot) => true;

        public GlobalTargetInfo Execute(GameEffect effect, Map map)
        {
            ThingDef def = DefDatabase<ThingDef>.GetNamed(effect.SelectedDefName[0], errorOnFail: false);
            if (def == null)
            {
                ModLogger.Warning($"ResourcePodCrash: ThingDef '{effect.DefNameText}' not found.");
                return GlobalTargetInfo.Invalid;
            }

            int quantity = ItemHandlerSupport.ResolveQuantity(def, effect.Severity, map);
            int pods = Rand.RangeInclusive(1, 4);
            var items = SplitIntoStacks(def, quantity, pods);

            IntVec3 cell = DropCellFinder.RandomDropSpot(map);
            DropPodUtility.DropThingsNear(cell, map, items);

            ModLogger.Message($"{Config.EffectName}: dropped {quantity}x {def.defName} " +
                              $"(value={effect.Severity}, pods={pods})");

            return new GlobalTargetInfo(cell, map);
        }

        public string GetSummary(GameEffect effect) =>
            $"{effect.DefNameText} dropped via pod ({effect.Severity})";

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
                    var thing = ThingMaker.MakeThing(def);
                    thing.stackCount = Math.Min(left, def.stackLimit);
                    items.Add(thing);
                    left -= thing.stackCount;
                }
            }
            return items;
        }
    }
}
