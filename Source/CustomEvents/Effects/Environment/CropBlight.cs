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
        private string executionSummary;

        public bool CanBeChoosen(ColonySnapshot snapshot) => snapshot?.cultivatedPlants != null && snapshot.cultivatedPlants.Any();

        public GlobalTargetInfo Execute(GameEffect effect, Map map)
        {
            IEnumerable<Plant> candidates = map.listerThings
                .ThingsInGroup(ThingRequestGroup.Plant)
                .OfType<Plant>()
                .Where(plant => plant.BlightableNow);

            string requestedCrop = effect.SelectedDefName != null && effect.SelectedDefName.Length > 0
                ? effect.SelectedDefName[0]
                : (!string.IsNullOrEmpty(effect.DefNameText) ? effect.DefNameText : "random");

            ThingDef cropDef = null;
            if (!string.IsNullOrEmpty(requestedCrop) && !requestedCrop.Equals("random", StringComparison.OrdinalIgnoreCase))
            {
                cropDef = DefDatabase<ThingDef>.GetNamed(requestedCrop, errorOnFail: false);
                if (cropDef != null)
                {
                    var specificCandidates = candidates.Where(plant => plant.def == cropDef).ToList();
                    if (specificCandidates.Count > 0)
                    {
                        candidates = specificCandidates;
                    }
                    else
                    {
                        ModLogger.Warning($"CropBlight: No BlightableNow plants of '{requestedCrop}' found, falling back to any eligible crops.");
                    }
                }
                else
                {
                    ModLogger.Warning($"CropBlight: Unknown plant def '{requestedCrop}', falling back to any eligible crop.");
                }
            }

            var candidateList = candidates.ToList();
            if (candidateList.Count == 0)
            {
                ModLogger.Warning("CropBlight: No BlightableNow crops found on map.");
                executionSummary = GetDefaultSummary(effect);
                return GlobalTargetInfo.Invalid;
            }

            int count = CalculateCropsAffected(candidateList.Count, effect.Severity);
            List<Plant> targets = candidateList.InRandomOrder().Take(count).ToList();

            if (targets.Count == 0)
            {
                targets = candidateList.InRandomOrder().Take(1).ToList();
            }

            foreach (Plant plant in targets)
            {
                plant.CropBlighted();
            }

            string cropLabel = cropDef != null
                ? cropDef.LabelCap.Resolve()
                : (targets.Count > 0 ? targets[0].def.LabelCap.Resolve() : "crops");

            string severityNote = effect.Severity != SeverityTypes.None ? $" ({effect.Severity.ToString().ToLower()} outbreak)" : "";
            executionSummary = $"A destructive crop blight has struck the fields: {targets.Count} {cropLabel} plant{(targets.Count > 1 ? "s were" : " was")} blighted{severityNote}.";

            ModLogger.Message($"CropBlight: Blighted {targets.Count} {cropLabel} plants.");
            return new GlobalTargetInfo(targets[0].Position, map);
        }

        private int CalculateCropsAffected(int candidates, SeverityTypes severity)
        {
            if (candidates <= 0) return 0;
            switch (severity)
            {
                case SeverityTypes.Low: return Math.Max(1, (int)Math.Ceiling(candidates * 0.10));
                case SeverityTypes.Medium: return Math.Max(2, (int)Math.Ceiling(candidates * 0.20));
                case SeverityTypes.High: return Math.Max(3, (int)Math.Ceiling(candidates * 0.35));
                case SeverityTypes.Extreme: return Math.Max(5, (int)Math.Ceiling(candidates * 0.60));
                default: return Math.Max(1, (int)Math.Ceiling(candidates * 0.15));
            }
        }

        public string GetSummary(GameEffect effect)
        {
            if (!string.IsNullOrEmpty(executionSummary))
                return executionSummary;

            return GetDefaultSummary(effect);
        }

        private static string GetDefaultSummary(GameEffect effect)
        {
            string requestedCrop = effect?.SelectedDefName != null && effect.SelectedDefName.Length > 0
                ? effect.SelectedDefName[0]
                : (!string.IsNullOrEmpty(effect?.DefNameText) ? effect.DefNameText : null);

            ThingDef cropDef = !string.IsNullOrEmpty(requestedCrop) && !requestedCrop.Equals("random", StringComparison.OrdinalIgnoreCase)
                ? DefDatabase<ThingDef>.GetNamed(requestedCrop, errorOnFail: false)
                : null;

            string cropLabel = cropDef != null ? cropDef.LabelCap.Resolve() : "crops";
            string sevStr = effect != null && effect.Severity != SeverityTypes.None ? $" ({effect.Severity.ToString().ToLower()} outbreak)" : "";

            return $"An agricultural blight outbreak has infected the colony's {cropLabel}{sevStr}. Cut infected plants before it spreads!";
        }
    }
}