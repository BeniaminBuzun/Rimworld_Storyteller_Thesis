using RimTelligence.DataSnapshots;
using RimWorld.Planet;
using System;
using System.Linq;
using Verse;

namespace RimTelligence.CustomEvents.Handlers.Item
{
    public class PawnFoundItems : IEffect
    {
        private readonly EffectConfig _config = EffectConfigRegistry.Get("PawnFoundItems");

        public EffectConfig Config => _config;

        public bool CanBeChoosen(ColonySnapshot snapshot) => true;

        private string executionSummary;

        public GlobalTargetInfo Execute(GameEffect effect, Map map)
        {
            ThingDef def = ResolveItemDef(effect);
            if (def == null || !ItemHandlerSupport.IsSpawnable(def))
            {
                ModLogger.Warning($"{Config.EffectName}: ThingDef '{effect.DefNameText}' not found.");
                executionSummary = "A colonist searched the area, but found nothing of value.";
                return GlobalTargetInfo.Invalid;
            }

            var allColonists = map.mapPawns.FreeColonistsSpawned.ToList();
            Pawn pawn = null;
            if (!string.IsNullOrWhiteSpace(effect.TargetPawn) && !effect.TargetPawn.Equals("random", StringComparison.OrdinalIgnoreCase))
            {
                pawn = allColonists.FirstOrDefault(candidate =>
                    candidate.LabelShort.Equals(effect.TargetPawn, StringComparison.OrdinalIgnoreCase));
            }

            if (pawn == null)
            {
                pawn = allColonists.RandomElementWithFallback();
            }

            if (pawn == null)
            {
                ModLogger.Warning($"{Config.EffectName}: no colonist found to receive items.");
                executionSummary = $"A supply cache of {def.LabelCap.Resolve()} was spotted nearby, but no colonists were available to retrieve it.";
                return GlobalTargetInfo.Invalid;
            }

            int quantity = ItemHandlerSupport.ResolveQuantity(def, effect.Severity, map);

            Thing item = ItemHandlerSupport.MakeItem(def, quantity);
            int placedCount = item.stackCount;   // read before placing: a merge can zero the stack

            bool placed = GenPlace.TryPlaceThing(item, pawn.Position, map, ThingPlaceMode.Near);
            if (!placed)
            {
                if (!item.Destroyed) item.Destroy();   // don't leak an unspawned Thing
                ModLogger.Warning($"{Config.EffectName}: no free cell near {pawn.LabelShort} for {def.defName}.");
                executionSummary = $"{pawn.LabelShort} spotted a cache of {def.LabelCap.Resolve()}, but there was no room to bring it back.";
                return GlobalTargetInfo.Invalid;
            }

            string itemLabel = def.LabelCap.Resolve();
            executionSummary = $"{pawn.LabelShort} scavenged the area and recovered {placedCount}x {itemLabel}!";

            ModLogger.Message($"{Config.EffectName}: gave {placedCount}x {def.defName} " +
                              $"to {pawn.LabelShort} (value={effect.Severity})");

            return new GlobalTargetInfo(pawn.Position, map);
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
            string pawn = !string.IsNullOrWhiteSpace(effect.TargetPawn) ? effect.TargetPawn : "A colonist";
            string sevStr = effect.Severity != SeverityTypes.None ? $" ({effect.Severity.ToString().ToLower()})" : "";
            return $"{pawn} discovers a salvageable cache of {itemLabel}{sevStr}.";
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
    }
}
