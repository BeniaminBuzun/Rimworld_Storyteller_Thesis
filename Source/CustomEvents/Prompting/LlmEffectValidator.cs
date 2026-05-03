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
            "PawnFoundItem",
            "DropAtRandomPlaceItems",
            "Disease",
            "FireCondition",
            "AnimalInsanity",
            "SelfTame",
            "SpawnPawns",
            "SpawnJoinable",
            "Alphabeavers",
        };

        public static List<LlmEffect> StripInvalid(List<LlmEffect> effects)
        {
            if (effects == null) return new List<LlmEffect>();

            var valid = new List<LlmEffect>();

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

        private static string GetRejectionReason(LlmEffect e)
        {
            if (string.IsNullOrWhiteSpace(e.type))
                return "type is blank";

            // Unknown type — handler won't exist, executor already warns but we drop early
            if (!Handlers.EffectHandlerRegistry.GetAllHandlers()
                    .Any(h => string.Equals(h.EffectType, e.type, System.StringComparison.OrdinalIgnoreCase)))
                return $"unknown effect type '{e.type}'";

            // Required defName missing
            if (_requiresDefName.Contains(e.type) && string.IsNullOrWhiteSpace(e.defName))
                return $"type '{e.type}' requires a defName but got empty string";

            // Pure filler: nothing meaningful was set
            bool noCount = e.count <= 0;
            bool noValue = e.value <= 0;
            bool noTarget = string.IsNullOrWhiteSpace(e.targetPawn);
            bool noDefName = string.IsNullOrWhiteSpace(e.defName);

            if (noCount && noValue && noTarget && noDefName)
                return "all fields are zero/empty — pure filler";

            return null;
        }
    }
}