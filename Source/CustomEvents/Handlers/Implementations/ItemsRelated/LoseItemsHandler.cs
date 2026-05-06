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
    public class LoseItemsHandler : IEffectHandler
    {
        public string EffectType => "LoseItems";

        public string PromptDescription => "- LoseItems      : defName = ThingDef (Silver, Wort, etc. ), count = amount";

        public string AditionalPromptInfo => "select only from itmes from Colony resources it can affect only items that are inside colony stockpile";

        public GlobalTargetInfo Execute(LlmEffect effect, Map map)
        {
            int leftToLose = effect.count;
            if (leftToLose <= 0 || string.IsNullOrWhiteSpace(effect.defName)) return GlobalTargetInfo.Invalid;

            foreach (Zone_Stockpile stockpile in map.zoneManager.AllZones.OfType<Zone_Stockpile>())
            {
                foreach (IntVec3 cell in stockpile.Cells)
                {
                    List<Thing> things = cell.GetThingList(map).ToList();
                    foreach (Thing thing in things)
                    {
                        if (leftToLose <= 0) return GlobalTargetInfo.Invalid;

                        if (string.Equals(thing.def.defName, effect.defName, StringComparison.OrdinalIgnoreCase))
                        {
                            int take = Math.Min(thing.stackCount, leftToLose);
                            thing.SplitOff(take).Destroy(DestroyMode.Vanish);
                            leftToLose -= take;
                        }
                    }
                }
            }
            return GlobalTargetInfo.Invalid;
        }

        public string GetSummary(LlmEffect effect) =>
            $"{effect.count} items(s) {effect.defName} lost";
        
    }
}
