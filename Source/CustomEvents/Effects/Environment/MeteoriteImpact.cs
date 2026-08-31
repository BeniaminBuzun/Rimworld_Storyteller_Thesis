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
    internal class MeteoriteImpact : IEffect
    {
        public EffectConfig Config => EffectConfigRegistry.Get("MeteoriteImpact");

        public bool CanBeChoosen(ColonySnapshot snapshot) => true;

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