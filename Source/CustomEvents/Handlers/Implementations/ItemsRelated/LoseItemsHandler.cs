using RimWorld;
using RimWorld.Planet;
using System;
using System.Linq;
using UnityEngine;
using Verse;

namespace RimTelligence.Source.CustomEvents.Handlers.Implementations.ItemsRelated
{
    public class LoseItemsHandler : GenericItemHandler
    {
        private readonly string _effectType;
        private readonly string _desc;
        private readonly float _baseWeight;
        private readonly EventAlignment _alignment;

        public LoseItemsHandler(string effectType, string desc, float baseWeight, EventAlignment alignment)
        {
            _effectType = effectType;
            _desc = desc;
            _baseWeight = baseWeight;
            _alignment = alignment;
        }

        public override string EffectName => _effectType;

        public override float BaseWeight => _baseWeight;
        public override EventAlignment Alignment => _alignment;

        public override string NarrativeDescription => $"An event where the {_desc}.";

        public override string PromptStructure =>
            $"- type = {_effectType}:\r\n" +
            $"    defName = \"ThingDef name\" (string) — {_desc}\r\n" +
            $"    count = 0\r\n" +
            $"    defDescription = \"\"\r\n" +
            $"    value = LOW | MEDIUM | HIGH\r\n" +
            $"    targetPawn = \"\"";

        public override GlobalTargetInfo Execute(GameEffect effect, Map map)
        {
            if (string.IsNullOrWhiteSpace(effect.defName))
                return GlobalTargetInfo.Invalid;

            ThingDef def = ValidateDef(effect);
            if (def == null) return GlobalTargetInfo.Invalid;

            int toRemove = WealthBasedQuantityResolver.Resolve(def, effect.value, map, fallbackCount: effect.count);
            int leftToLose = toRemove;

            foreach (Zone_Stockpile stockpile in map.zoneManager.AllZones.OfType<Zone_Stockpile>())
            {
                foreach (IntVec3 cell in stockpile.Cells)
                {
                    if (leftToLose <= 0) break;

                    foreach (Thing thing in cell.GetThingList(map).ToList())
                    {
                        if (leftToLose <= 0) break;
                        if (!string.Equals(thing.def.defName, effect.defName,
                                StringComparison.OrdinalIgnoreCase)) continue;

                        int take = Math.Min(thing.stackCount, leftToLose);
                        thing.SplitOff(take).Destroy(DestroyMode.Vanish);
                        leftToLose -= take;
                    }
                }
            }

            int removed = toRemove - leftToLose;
            ModLogger.Message($"{EffectName}: removed {removed}x {effect.defName} " +
                              $"(value={effect.value}, requested={toRemove})");

            return GlobalTargetInfo.Invalid;
        }

        public override string GetSummary(GameEffect effect) =>
            $"{effect.defName} lost ({effect.value})";
    }
}