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
        public string EffectName => "CropBlight";

        public string NarrativeDescription => $"An event where colony plants are affected by blight.";

        public string PromptStructure => "- type = CropBlight:\r\n    defName = \"ThingDef name\" (string) or random\r\n    count = integer (number of animals)\r\n    defDescription = \"\"\r\n    value = NONE\r\n    targetPawn = \"\"";

        public string AditionalPromptStructureInfo => "select defName only from harvestable_plants list."; // may probably not work I don't know is this list still exist it should but test

        public float BaseWeight => 5f;

        public EventAlignment Alignment => EventAlignment.Bad;

        public GlobalTargetInfo Execute(GameEffect effect, Map map)
        {
            int count = Math.Max(1, effect.count);

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

        public string GetSummary(GameEffect effect) =>
            $"{effect.count} {(effect.defName.Equals("random", StringComparison.OrdinalIgnoreCase) ? "random crop(s)" : effect.defName)} blighted";
    }
}