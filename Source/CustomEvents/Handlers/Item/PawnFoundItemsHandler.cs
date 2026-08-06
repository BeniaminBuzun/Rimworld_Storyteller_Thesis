using RimWorld;
using RimWorld.Planet;
using System;
using System.Linq;
using UnityEngine;
using Verse;

namespace RimTelligence.CustomEvents.Handlers.Item
{
    internal class PawnFoundItemsHandler : GenericItemHandler
    {
        private readonly string _effectType;
        private readonly string _desc;
        private readonly float _baseWeight;
        private readonly EventAlignment _alignment;

        public PawnFoundItemsHandler(string effectType, string desc, float baseWeight, EventAlignment alignment)
        {
            _effectType = effectType;
            _desc = desc;
            _baseWeight = baseWeight;
            _alignment = alignment;
        }

        public override string EffectName => _effectType;

        public override float BaseWeight => _baseWeight;
        public override EventAlignment Alignment => _alignment;

        public override string NarrativeDescription => $"An event where a {_desc}.";

        public override string PromptStructure =>
            $"- type = {_effectType}:\r\n" +
            $"    defName = \"ThingDef name\" (string) — {_desc}\r\n" +
            $"    count = 0\r\n" +
            $"    defDescription = \"\"\r\n" +
            $"    value = LOW | MEDIUM | HIGH\r\n" +
            $"    targetPawn = \"colonist name\"";

        public override GlobalTargetInfo Execute(GameEffect effect, Map map)
        {
            ThingDef def = ValidateDef(effect);
            if (def == null) return GlobalTargetInfo.Invalid;

            Pawn pawn = map.mapPawns.FreeColonists
                .FirstOrDefault(candidate => candidate.Name.ToStringShort.Equals(
                    effect.targetPawn, StringComparison.OrdinalIgnoreCase));

            if (pawn == null)
            {
                ModLogger.Warning($"{EffectName}: colonist '{effect.targetPawn}' not found.");
                return GlobalTargetInfo.Invalid;
            }

            int quantity = WealthBasedQuantityResolver.Resolve(def, effect.value, map, fallbackCount: effect.count);

            Thing item = ThingMaker.MakeThing(def);
            item.stackCount = Math.Min(Math.Max(quantity, 1), def.stackLimit);

            GenPlace.TryPlaceThing(item, pawn.Position, map, ThingPlaceMode.Near);

            ModLogger.Message($"{EffectName}: gave {item.stackCount}x {def.defName} " +
                              $"to {pawn.LabelShort} (value={effect.value})");

            return new GlobalTargetInfo(pawn.Position, map);
        }

        public override string GetSummary(GameEffect effect) =>
            $"{effect.defName} found by {effect.targetPawn} ({effect.value})";
    }
}