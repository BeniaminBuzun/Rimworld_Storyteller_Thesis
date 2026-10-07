using RimTelligence.DataSnapshots;
using RimWorld.Planet;
using System;
using System.Linq;
using Verse;

namespace RimTelligence.CustomEvents.Handlers.Item
{
    public class PawnFoundItems : IEffect
    {
        public EffectConfig Config => EffectConfigRegistry.Get("PawnFoundItems");

        public bool CanBeChoosen(ColonySnapshot snapshot) => true;

        public GlobalTargetInfo Execute(GameEffect effect, Map map)
        {
            ThingDef def = DefDatabase<ThingDef>.GetNamed(effect.SelectedDefName[0], errorOnFail: false);
            if (def == null)
            {
                ModLogger.Warning($"{Config.EffectName}: ThingDef '{effect.DefNameText}' not found.");
                return GlobalTargetInfo.Invalid;
            }

            Pawn pawn = map.mapPawns.FreeColonists
                .FirstOrDefault(candidate => candidate.Name.ToStringShort.Equals(
                    effect.TargetPawn, StringComparison.OrdinalIgnoreCase));

            if (pawn == null)
            {
                ModLogger.Warning($"{Config.EffectName}: colonist '{effect.TargetPawn}' not found.");
                return GlobalTargetInfo.Invalid;
            }

            int quantity = ItemHandlerSupport.ResolveQuantity(def, effect.Severity, map);

            Thing item = ThingMaker.MakeThing(def);
            item.stackCount = Math.Min(Math.Max(quantity, 1), def.stackLimit);

            GenPlace.TryPlaceThing(item, pawn.Position, map, ThingPlaceMode.Near);

            ModLogger.Message($"{Config.EffectName}: gave {item.stackCount}x {def.defName} " +
                              $"to {pawn.LabelShort} (value={effect.Severity})");

            return new GlobalTargetInfo(pawn.Position, map);
        }

        public string GetSummary(GameEffect effect) =>
            $"{effect.DefNameText} found by {effect.TargetPawn} ({effect.Severity})";
    }
}
