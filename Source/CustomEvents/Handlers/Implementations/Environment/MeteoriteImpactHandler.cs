using RimTelligence.Source.DataSnapshots;
using RimWorld;
using RimWorld.Planet;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Verse;

namespace RimTelligence.Source.CustomEvents.Handlers.Implementations.Environment
{
    internal class MeteoriteImpactHandler : IEffectHandler
    {
        public string EffectType => "MeteoriteImpact";

        public string PromptDescription => "- MeteoriteImpact        :";

        public string AditionalPromptInfo => $"";

        public GlobalTargetInfo Execute(LlmEffect effect, Map map)
        {
            IntVec3 cell = DropCellFinder.RandomDropSpot(map);

            List<Thing> things = ThingSetMakerDefOf.Meteorite.root.Generate();

            SkyfallerMaker.SpawnSkyfaller(ThingDefOf.MeteoriteIncoming, things, cell, map);

            ModLogger.Message("Meteorite impact triggered.");

            return new GlobalTargetInfo(cell, map);
        }

        public string GetSummary(LlmEffect effect)
        {
            return "Meteorite has landed somewhere.";
        }
    }
}
