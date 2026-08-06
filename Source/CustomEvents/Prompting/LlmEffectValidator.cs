using System.Collections.Generic;
using System.Linq;

namespace RimTelligence.CustomEvents.Prompting
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

            foreach (var effect in effects)
            {
                string reason = GetRejectionReason(effect);
                if (reason != null)
                {
                    ModLogger.Warning($"[Validator] Dropping effect type='{effect.type}' defName='{effect.defName}': {reason}");
                    continue;
                }
                valid.Add(effect);
            }

            return valid;
        }

        private static string GetRejectionReason(GameEffect effect)
        {
            if (string.IsNullOrWhiteSpace(effect.type))
                return "type is blank";

            // Unknown type — handler won't exist, executor already warns but we drop early
            if (!Handlers.EffectHandlerRegistry.GetAllHandlers()
                    .Any(handler => string.Equals(handler.EffectName, effect.type, System.StringComparison.OrdinalIgnoreCase)))
                return $"unknown effect type '{effect.type}'";

            // Required defName missing
            if (_requiresDefName.Contains(effect.type) && string.IsNullOrWhiteSpace(effect.defName))
                return $"type '{effect.type}' requires a defName but got empty string";

            // Pure filler: nothing meaningful was set
            bool noCount = effect.count <= 0;
            bool noValue = effect.value <= 0;
            bool noTarget = string.IsNullOrWhiteSpace(effect.targetPawn);
            bool noDefName = string.IsNullOrWhiteSpace(effect.defName);

            if (noCount && noValue && noTarget && noDefName && effect.defName != "MeteoriteImpact")
                return "all fields are zero/empty — pure filler";

            return null;
        }
    }
}