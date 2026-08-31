using RimTelligence.CustomEvents.Handlers;
using RimTelligence.DataSnapshots;
using RimTelligence.Source.CustomEvents.Handlers;
using System;
using System.Collections.Generic;
using System.Linq;

namespace RimTelligence.CustomEvents.Prompting
{
    public static class LlmEffectValidator
    {
        public static List<GameEffect> StripInvalid(
    List<GameEffect> effects,
    IEnumerable<IEffect> chosenEvents = null)
        {
            List<GameEffect> valid = new List<GameEffect>();
            if (effects == null) return valid;

            Dictionary<string, int> allowedCounts = chosenEvents == null
                ? null
                : chosenEvents
                    .Where(e => e?.Config != null)
                    .GroupBy(e => e.Config.EffectName, StringComparer.OrdinalIgnoreCase)
                    .ToDictionary(group => group.Key, group => group.Count(), StringComparer.OrdinalIgnoreCase);

            foreach (GameEffect effect in effects)
            {
                string reason = GetRejectionReason(effect, allowedCounts);
                if (reason != null)
                {
                    ModLogger.Warning($"[Validator] Dropping effect type='{effect.EffectName}' defName='{effect.DefNameText}': {reason}");
                    continue;
                }
                valid.Add(effect);
            }
            return valid;
        }

        private static string GetRejectionReason(GameEffect effect, Dictionary<string, int> allowedCounts)
        {
            if (effect == null) return "effect is null";
            if (string.IsNullOrWhiteSpace(effect.EffectName)) return "type is blank";

            IEffect handler = EffectFactory.Create(effect.EffectName);
            if (handler == null)
                return $"unknown effect type '{effect.EffectName}'";

            if (allowedCounts != null)
            {
                if (!allowedCounts.TryGetValue(effect.EffectName, out int remaining) || remaining <= 0)
                    return $"effect type '{effect.EffectName}' was not in the requested cue or was repeated more than cued";
                allowedCounts[effect.EffectName] = remaining - 1;
            }

            if (handler.Config.IsDefRequired)
            {
                if (effect.SelectedDefName == null || effect.SelectedDefName.Length == 0 ||
                    string.IsNullOrWhiteSpace(effect.SelectedDefName[0]))
                    return $"type '{effect.EffectName}' requires a defName but got empty string";

                if (handler.Config.DefCount > 1 && effect.SelectedDefName.Length < handler.Config.DefCount)
                    return $"type '{effect.EffectName}' requires {handler.Config.DefCount} defNames but got {effect.SelectedDefName.Length}";

                var contract = EffectContractProvider.GetContract(handler, null);
                for (int i = 0; i < effect.SelectedDefName.Length; i++)
                {
                    string given = effect.SelectedDefName[i];
                    if (string.IsNullOrWhiteSpace(given))
                        return $"type '{effect.EffectName}' has a blank defName at index {i}";


                    List<GameObjectMeta> candidates = i == 0
                        ? contract.AllowedDefs
                        : contract.SecondaryAllowedDefs.Count > 0
                            ? contract.SecondaryAllowedDefs
                            : contract.AllowedDefs;

                    if (candidates.Count == 0)
                        continue; // snapshot-driven/empty list — nothing verifiable, keep the def

                    GameObjectMeta match = candidates.FirstOrDefault(d =>
                        string.Equals(d.DefName, given, StringComparison.OrdinalIgnoreCase) ||
                        string.Equals(d.Label, given, StringComparison.OrdinalIgnoreCase));

                    if (match == null)
                        return $"defName '{given}' not in allowed list for '{effect.EffectName}' (checked DefName and Label, case-insensitive)";

                    effect.SelectedDefName[i] = match.DefName;
                }
            }

            return null;
        }
    }
}
