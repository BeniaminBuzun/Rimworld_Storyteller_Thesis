using RimTelligence.DataSnapshots;
using RimWorld;
using RimWorld.Planet;
using System.Collections.Generic;
using System.Linq;
using Verse;

namespace RimTelligence.CustomEvents.Handlers.Environment
{
    internal class MeteoriteImpact : IEffect
    {
        private readonly EffectConfig _config = EffectConfigRegistry.Get("MeteoriteImpact");

        public EffectConfig Config => _config;

        public bool CanBeChoosen(ColonySnapshot snapshot) => true;

        private string executionSummary;

        public GlobalTargetInfo Execute(GameEffect effect, Map map)
        {
            IntVec3 cell = DropCellFinder.RandomDropSpot(map);

            List<Thing> things = ThingSetMakerDefOf.Meteorite.root.Generate();

            SkyfallerMaker.SpawnSkyfaller(ThingDefOf.MeteoriteIncoming, things, cell, map);

            string deposit = (things != null && things.Count > 0)
                ? string.Join(", ", things.Select(t => t.def.LabelCap.Resolve()).Distinct())
                : "precious ores";

            executionSummary = $"A blazing meteorite crashed down from orbit, leaving an impact crater and a deposit of {deposit}!";
            ModLogger.Message("Meteorite impact triggered.");

            return new GlobalTargetInfo(cell, map);
        }

        public string GetSummary(GameEffect effect)
        {
            if (!string.IsNullOrEmpty(executionSummary))
                return executionSummary;

            return GetDefaultSummary(effect);
        }

        private static string GetDefaultSummary(GameEffect effect)
        {
            return "A blazing meteorite crashed down from orbit, slamming into the terrain with rich mineral deposits.";
        }
    }
}