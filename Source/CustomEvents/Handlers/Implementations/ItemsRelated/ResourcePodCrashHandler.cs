using Mono.Security.Cryptography;
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
    internal class ResourcePodCrashHandler : IEffectHandler
    {
        public string EffectType => "ResourcePodCrash";
        public string PromptDescription => "- ResourcePodCrash      : defName = ThingDef (Silver, Wort, etc. ), count = amount";

        public string AditionalPromptInfo => $"select only from these itmes {string.Join(", ", DataList.ResourceList)} {string.Join(", ", DataList.FoodList)}";

        public GlobalTargetInfo Execute(LlmEffect effect, Map map)
        {

            ThingDef def = DefDatabase<ThingDef>.GetNamed(effect.defName, errorOnFail: false);
            if (def == null)
            {
                ModLogger.Warning($"ResourcePodCrash: ThingDef '{effect.defName}' not found.");
                return GlobalTargetInfo.Invalid;
            }

            int numOfDropPods = Rand.RangeInclusive(1, 4);

            int baseCount = effect.count / numOfDropPods;
            int remainder = effect.count % numOfDropPods;



            List<Thing> items = new List<Thing>();

            for (int i =0; i<numOfDropPods; i++)
            {
                int thisStack = baseCount + (i < remainder ? 1 : 0);
                int remaining = thisStack;

                while (remaining > 0)
                {
                    Thing item = ThingMaker.MakeThing(def);
                    int stack = Math.Min(remaining, def.stackLimit);
                    item.stackCount = stack;
                    items.Add(item);
                    remaining -= stack;
                }
            }
            
            IntVec3 dropCell = DropCellFinder.RandomDropSpot(map);

            DropPodUtility.DropThingsNear(dropCell, map, items);

            return new GlobalTargetInfo(dropCell, map);

        }

        public string GetSummary(LlmEffect effect) => $"{effect.count}x {effect.defName} dropped";
    }
}
