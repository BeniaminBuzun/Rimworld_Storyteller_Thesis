using System.Collections.Generic;
using System.Linq;

namespace RimTelligence.Source.CustomEvents
{
    public static class LlmEffectValidator
    {
        // Types where an empty defName makes execution meaningless or will throw.
        private static readonly HashSet<string> _requiresDefName = new HashSet<string>(
            System.StringComparer.OrdinalIgnoreCase)
        {
            "AnimalInsanity",
            "SelfTame",
            "ChangeWeather",
            "CropBlight",
            "StartExtremeWeatherEvent",
            "LoseItems",
            "PawnFoundItem",
            "ResourcePodCrash",
            "SpawnItemsAtRandomLocation",
            "AddExpPoints",
            "MoodDebuff",
            "MoodBuff",
            "DiseasePawn",
            "DamagePawn",
            "LoseExpPoints",
            "MiracleHealChronicCondition",
            "MiracleHealDisease",
            "MiracleHealMissingPart"
        };

        public static List<GameEffect> StripInvalid(List<GameEffect> effects)
        {
            if (effects == null) return new List<GameEffect>();

            var valid = new List<GameEffect>();

            foreach (var e in effects)
            {
                string reason = GetRejectionReason(e);
                if (reason != null)
                {
                    ModLogger.Warning($"[Validator] Dropping effect type='{e.type}' defName='{e.defName}': {reason}");
                    continue;
                }
                valid.Add(e);
            }

            return valid;
        }

        private static string GetRejectionReason(GameEffect e)
        {
            if (string.IsNullOrWhiteSpace(e.type))
                return "type is blank";

            // Unknown type — handler won't exist, executor already warns but we drop early
            if (!Handlers.EffectHandlerRegistry.GetAllHandlers()
                    .Any(h => string.Equals(h.EffectName, e.type, System.StringComparison.OrdinalIgnoreCase)))
                return $"unknown effect type '{e.type}'";

            // Required defName missing
            if (_requiresDefName.Contains(e.type) && string.IsNullOrWhiteSpace(e.defName))
                return $"type '{e.type}' requires a defName but got empty string";

            // Pure filler: nothing meaningful was set
            bool noCount = e.count <= 0;
            bool noValue = e.value <= 0;
            bool noTarget = string.IsNullOrWhiteSpace(e.targetPawn);
            bool noDefName = string.IsNullOrWhiteSpace(e.defName);

            if (noCount && noValue && noTarget && noDefName && e.defName != "MeteoriteImpact")
                return "all fields are zero/empty — pure filler";

            return null;
        }
    }
}