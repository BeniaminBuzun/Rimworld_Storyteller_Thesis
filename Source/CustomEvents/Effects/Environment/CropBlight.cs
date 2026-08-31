using RimTelligence.DataSnapshots;
using RimWorld;
using RimWorld.Planet;
using System;
using System.Collections.Generic;
using System.Linq;
using Verse;

namespace RimTelligence.CustomEvents.Handlers.Environment
{
    public class CropBlight : IEffect
    {
        public EffectConfig Config => EffectConfigRegistry.Get("CropBlight");

        public bool CanBeChoosen(ColonySnapshot snapshot) => snapshot.cultivatedPlants.Any();

        public GlobalTargetInfo Execute(GameEffect effect, Map map)
        {
            IEnumerable<Plant> candidates = map.listerThings
                .ThingsInGroup(ThingRequestGroup.Plant)
                .OfType<Plant>()
                .Where(plant => plant.BlightableNow);

            string requestedCrop = effect.SelectedDefName != null && effect.SelectedDefName.Length > 0
                ? effect.SelectedDefName[0]
                : "random";

            if (!requestedCrop.Equals("random", StringComparison.OrdinalIgnoreCase))
            {
                ThingDef cropDef = DefDatabase<ThingDef>.GetNamed(requestedCrop, errorOnFail: false);
                if (cropDef != null)
                {
                    candidates = candidates.Where(plant => plant.def == cropDef);
                }
                else
                {
                    ModLogger.Warning($"CropBlight: Unknown plant def '{requestedCrop}', falling back to any eligible crop.");
                }
            }

            int count = CalculateCropsAffected(candidates.Count(), effect.Severity);

            List<Plant> targets = candidates.InRandomOrder().Take(count).ToList();

            if (targets.Count == 0)
            {
                ModLogger.Warning($"CropBlight: No BlightableNow crops found for '{requestedCrop}'.");
                return GlobalTargetInfo.Invalid;
            }

            foreach (Plant plant in targets)
            {
                plant.CropBlighted();
            }

            ModLogger.Message($"CropBlight: Blighted {targets.Count} {requestedCrop}.");

            return new GlobalTargetInfo(targets[0].Position, map);
        }

        private int CalculateCropsAffected(int candidates, SeverityTypes severity)
        {
            switch (severity)
            {
                case SeverityTypes.Low: return (int)Math.Ceiling(candidates * 0.05);
                case SeverityTypes.Medium: return (int)Math.Ceiling(candidates * 0.10);
                case SeverityTypes.High: return (int)Math.Ceiling(candidates * 0.25);
                case SeverityTypes.Extreme: return (int)Math.Ceiling(candidates * 0.5);
                default: return 0;
            }
        }

        public string GetSummary(GameEffect effect) =>
            $"{effect.DefNameText} blighted";
    }
}