using RimTelligence.DataSnapshots;
using RimWorld;
using RimWorld.Planet;
using System;
using System.Linq;
using UnityEngine;
using Verse;

namespace RimTelligence.CustomEvents.Handlers.Item
{
    public class LoseItems : IEffect
    {
        public EffectConfig Config => EffectConfigRegistry.Get("LoseItems");

        public bool CanBeChoosen(ColonySnapshot snapshot) => true;

        public GlobalTargetInfo Execute(GameEffect effect, Map map)
        {
            if (effect.SelectedDefName == null || effect.SelectedDefName.Length == 0 ||
                string.IsNullOrWhiteSpace(effect.SelectedDefName[0]))
                return GlobalTargetInfo.Invalid;

            ThingDef def = DefDatabase<ThingDef>.GetNamed(effect.SelectedDefName[0], errorOnFail: false);
            if (def == null)
            {
                ModLogger.Message($"{Config.EffectName}: unrecognized item type '{effect.DefNameText}'; nothing was removed.");
                return GlobalTargetInfo.Invalid;
            }

            int toRemove = ItemHandlerSupport.ResolveQuantity(def, effect.Severity, map);
            int leftToLose = toRemove;

            foreach (Zone_Stockpile stockpile in map.zoneManager.AllZones.OfType<Zone_Stockpile>())
            {
                foreach (IntVec3 cell in stockpile.Cells)
                {
                    if (leftToLose <= 0) break;

                    foreach (Thing thing in cell.GetThingList(map).ToList())
                    {
                        if (leftToLose <= 0) break;
                        if (!string.Equals(thing.def.defName, effect.SelectedDefName[0],
                                StringComparison.OrdinalIgnoreCase)) continue;

                        int take = Math.Min(thing.stackCount, leftToLose);
                        thing.SplitOff(take).Destroy(DestroyMode.Vanish);
                        leftToLose -= take;
                    }
                }
            }

            int removed = toRemove - leftToLose;
            ModLogger.Message($"{Config.EffectName}: removed {removed}x {effect.DefNameText} " +
                              $"(value={effect.Severity}, requested={toRemove})");

            return GlobalTargetInfo.Invalid;
        }

        public string GetSummary(GameEffect effect) =>
            $"{effect.DefNameText} lost ({effect.Severity})";
    }
}