using RimTelligence.DataSnapshots;
using RimWorld;
using RimWorld.Planet;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Verse;

namespace RimTelligence.CustomEvents.Handlers.Environment
{
    internal class MeteoriteImpactHandler : IEffectHandler
    {
        public string EffectName => "MeteoriteImpact";

        public string NarrativeDescription => $"An event where a meteorite impacts the map.";

        public string PromptStructure => "- type = MeteoriteImpact:\r\n    defName =\"\" \r\n    count = 10\r\n    defDescription = \"\"\r\n    value = NONE\r\n    targetPawn = \"\"";

        public string AditionalPromptStructureInfo => $"";

        public float BaseWeight => 5f;

        public EventAlignment Alignment => EventAlignment.Neutral;

        public GlobalTargetInfo Execute(GameEffect effect, Map map)
        {
            IntVec3 cell = DropCellFinder.RandomDropSpot(map);

            List<Thing> things = ThingSetMakerDefOf.Meteorite.root.Generate();

            SkyfallerMaker.SpawnSkyfaller(ThingDefOf.MeteoriteIncoming, things, cell, map);

            ModLogger.Message("Meteorite impact triggered.");

            return new GlobalTargetInfo(cell, map);
        }

        public string GetSummary(GameEffect effect)
        {
            return "Meteorite has landed somewhere.";
        }
    }
}