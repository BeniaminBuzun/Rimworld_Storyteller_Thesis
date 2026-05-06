using RimTelligence.Source.DataSnapshots;
using RimWorld;
using RimWorld.Planet;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;

namespace RimTelligence.Source.CustomEvents.Handlers.Implementations.ItemsRelated
{
    internal class SpawnItemsAtRandomLocationHandler : IEffectHandler
    {
        public string EffectType => "SpawnItemsAtRandomLocation";
        public string PromptDescription => "- SpawnItemsAtRandomLocation      : defName = ThingDef (Silver, Wort, etc. ), count = amount";

        public string AditionalPromptInfo => $"select only from these itmes {string.Join(", ", DataList.ResourceList)}";
        public GlobalTargetInfo Execute(LlmEffect effect, Map map)
        {
            ThingDef def = DefDatabase<ThingDef>.GetNamed(effect.defName, errorOnFail: false);
            if (def == null)
            {
                ModLogger.Warning($"SpawnItemsAtRandomLocation: ThingDef '{effect.defName}' not found.");
                return GlobalTargetInfo.Invalid;
            }

            Thing item = ThingMaker.MakeThing(def);
            item.stackCount = Math.Min(effect.count, def.stackLimit);

            IntVec3 dropCell = DropCellFinder.RandomDropSpot(map);
            GenPlace.TryPlaceThing(item, dropCell, map, ThingPlaceMode.Near);
            ModLogger.Message($"SpawnItemsAtRandomLocation {item.stackCount}x {def.defName} at {dropCell}");

            return new GlobalTargetInfo(dropCell, map);
        }

        public string GetSummary(LlmEffect effect) => $"{effect.count}x {effect.defName} appeared on the map";
    }
}
