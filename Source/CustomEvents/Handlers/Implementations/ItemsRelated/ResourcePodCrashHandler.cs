using RimWorld;
using RimWorld.Planet;
using System;
using System.Collections.Generic;
using Verse;

namespace RimTelligence.Source.CustomEvents.Handlers.Implementations.ItemsRelated
{
    internal class ResourcePodCrashHandler : GenericItemHandler
    {
        private readonly string _effectType;
        private readonly string _desc;
        private readonly float _baseWeight;
        private readonly EventAlignment _alignment;

        public ResourcePodCrashHandler(string effectType, string desc, float baseWeight, EventAlignment alignment)
        {
            _effectType = effectType;
            _desc = desc;
            _baseWeight = baseWeight;
            _alignment = alignment;
        }

        public override string EffectName => _effectType;

        public override float BaseWeight => _baseWeight;
        public override EventAlignment Alignment => _alignment;

        public override string NarrativeDescription => $"An event where {_desc} providing additional items for colony.";


        public override string PromptStructure =>
            $"- type = {_effectType}:\r\n" +
            $"    defName = \"ThingDef name\" (string) — {_desc}\r\n" +
            $"    count = 0\r\n" +
            $"    defDescription = \"\"\r\n" +
            $"    value = LOW | MEDIUM | HIGH\r\n" +
            $"    targetPawn = \"\"";

        public override GlobalTargetInfo Execute(GameEffect effect, Map map)
        {
            ThingDef def = ValidateDef(effect);
            if (def == null) return GlobalTargetInfo.Invalid;

            // Derive quantity from colony wealth + value tier instead of raw count.
            int quantity = WealthBasedQuantityResolver.Resolve(def, effect.value, map, fallbackCount: effect.count);

            int pods = Rand.RangeInclusive(1, 4);
            var items = SplitIntoStacks(def, quantity, pods);

            IntVec3 cell = DropCellFinder.RandomDropSpot(map);
            DropPodUtility.DropThingsNear(cell, map, items);

            ModLogger.Message($"{EffectName}: dropped {quantity}x {def.defName} " +
                              $"(value={effect.value}, pods={pods})");

            return new GlobalTargetInfo(cell, map);
        }

        public override string GetSummary(GameEffect e) =>
            $"{e.defName} dropped via pod ({e.value})";

        //
        //
        //
        //
        //
        //  helpers 

        private static List<Thing> SplitIntoStacks(ThingDef def, int total, int pods)
        {
            var items = new List<Thing>();
            int base_ = total / pods;
            int rem = total % pods;

            for (int i = 0; i < pods; i++)
            {
                int count = base_ + (i < rem ? 1 : 0);
                int left = count;

                while (left > 0)
                {
                    var t = ThingMaker.MakeThing(def);
                    t.stackCount = Math.Min(left, def.stackLimit);
                    items.Add(t);
                    left -= t.stackCount;
                }
            }
            return items;
        }
    }
}