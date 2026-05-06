using RimWorld;
using RimWorld.Planet;
using System;
using System.Collections.Generic;
using System.Linq;
using Verse;

namespace RimTelligence.Source.CustomEvents.Handlers
{
    public class CropBlightHandler : IEffectHandler
    {
        public string EffectType => "CropBlight";

        public string PromptDescription => "- CropBlight    : defName = plant ThingDef to blight (e.g. Plant_Corn, Plant_Rice) or \"random\", value = number of plants to blight, targetPawn is unused";

        public string AditionalPromptInfo => "select defName only from harvestable_plants list.";

        public GlobalTargetInfo Execute(LlmEffect effect, Map map)
        {
            int count = Math.Max(1, (int)effect.value);

            IEnumerable<Plant> candidates = map.listerThings
                .ThingsInGroup(ThingRequestGroup.Plant)
                .OfType<Plant>()
                .Where(p => p.BlightableNow);

            if (!effect.defName.Equals("random", StringComparison.OrdinalIgnoreCase))
            {
                ThingDef cropDef = DefDatabase<ThingDef>.GetNamed(effect.defName, errorOnFail: false);
                if (cropDef != null)
                {
                    candidates = candidates.Where(p => p.def == cropDef);
                }
                else
                {
                    ModLogger.Warning($"CropBlight: Unknown plant def '{effect.defName}', falling back to any eligible crop.");
                }
            }

            List<Plant> targets = candidates.InRandomOrder().Take(count).ToList();

            if (targets.Count == 0)
            {
                ModLogger.Warning($"CropBlight: No BlightableNow crops found for '{effect.defName}'.");
                return GlobalTargetInfo.Invalid;
            }

            foreach (Plant plant in targets)
            {
                plant.CropBlighted();
            }

            string cropLabel = effect.defName.Equals("random", StringComparison.OrdinalIgnoreCase)
                ? "random crop(s)"
                : effect.defName;

            ModLogger.Message($"CropBlight: Blighted {targets.Count} {cropLabel}.");

            return new GlobalTargetInfo(targets[0].Position, map);
        }

        public string GetSummary(LlmEffect effect) =>
            $"{effect.value} {(effect.defName.Equals("random", StringComparison.OrdinalIgnoreCase) ? "random crop(s)" : effect.defName)} blighted";
    }
}