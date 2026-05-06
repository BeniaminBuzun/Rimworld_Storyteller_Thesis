using RimTelligence.Source.DataSnapshots;
using RimWorld;
using RimWorld.Planet;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;

namespace RimTelligence.Source.CustomEvents.Handlers.Implementations
{
    internal class PawnFoundItemsHandler : IEffectHandler
    {
        public string EffectType => "PawnFoundItem";
        public string PromptDescription => "- PawnFoundItem      : defName = ThingDef (Silver, Wort, etc. ), count = amount, targetPawn = name";

        public string AditionalPromptInfo => $"select only from these itmes {string.Join(", ", DataList.ResourceList)}";
        public GlobalTargetInfo Execute(LlmEffect effect, Map map)
        {
            ThingDef def = DefDatabase<ThingDef>.GetNamed(effect.defName, errorOnFail: false);
            if (def == null)
            {
                ModLogger.Warning($"PawnFoundItems: ThingDef '{effect.defName}' not found.");
                return GlobalTargetInfo.Invalid;
            }

            Thing item = ThingMaker.MakeThing(def);
            item.stackCount = Math.Min(effect.count, def.stackLimit);

            Pawn pawn = map.mapPawns.FreeColonists.FirstOrDefault(p => p.Name.ToStringShort == effect.targetPawn);
            IntVec3 dropCell = pawn.Position;
            GenPlace.TryPlaceThing(item, dropCell, map, ThingPlaceMode.Near);
            ModLogger.Message($"Gave {item.stackCount}x {def.defName} at {dropCell}");

            return new GlobalTargetInfo(dropCell, map);
        }

        public string GetSummary(LlmEffect effect) => $"{effect.count} x {effect.defName} found by {effect.targetPawn} ";
    }
}
