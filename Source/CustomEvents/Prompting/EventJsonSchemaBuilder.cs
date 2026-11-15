using RimTelligence.CustomEvents.Handlers;
using RimTelligence.DataSnapshots;
using RimTelligence.Source.CustomEvents.Handlers;
using System;
using System.Collections.Generic;
using System.Linq;

namespace RimTelligence.CustomEvents.Prompting
{
    public static class EventJsonSchemaBuilder
    {
        public static object Build(List<IEffect> chosenEvents, ColonySnapshot snapshot)
        {
            var distinctEvents = (chosenEvents ?? Enumerable.Empty<IEffect>())
                .Where(e => e != null)
                .GroupBy(e => e.Config?.EffectName ?? string.Empty, StringComparer.OrdinalIgnoreCase)
                .Where(g => !string.IsNullOrEmpty(g.Key))
                .Select(g => g.First())
                .ToList();

            List<object> effectSchemas;

            if (distinctEvents.Count > 0)
            {
                effectSchemas = distinctEvents
                    .Select(effect => BuildEffectSchema(EffectContractProvider.GetContract(effect, snapshot)))
                    .Cast<object>()
                    .ToList();
            }
            else
            {
                // Fallback: when chosenEvents is empty, allow any registered effect rather than producing anyOf: []
                effectSchemas = EffectFactory.GetAllEffectNames()
                    .Select(name =>
                    {
                        try { return EffectFactory.Create(name); }
                        catch { return null; }
                    })
                    .Where(e => e != null)
                    .GroupBy(e => e.Config?.EffectName ?? string.Empty, StringComparer.OrdinalIgnoreCase)
                    .Where(g => !string.IsNullOrEmpty(g.Key))
                    .Select(g => BuildEffectSchema(EffectContractProvider.GetContract(g.First(), snapshot)))
                    .Cast<object>()
                    .ToList();
            }

            object itemsSchema;
            if (effectSchemas.Count == 1)
            {
                itemsSchema = effectSchemas[0];
            }
            else if (effectSchemas.Count > 1)
            {
                itemsSchema = new Dictionary<string, object> { ["anyOf"] = effectSchemas };
            }
            else
            {
                itemsSchema = BuildGenericFallbackSchema();
            }

            Dictionary<string, object> effectsSchema = new Dictionary<string, object>
            {
                ["type"] = "array",
                ["items"] = itemsSchema
            };

            Dictionary<string, object> properties = new Dictionary<string, object>
            {
                ["letterTitle"] = new Dictionary<string, object> { ["type"] = "string" },
                ["letterText"] = new Dictionary<string, object> { ["type"] = "string" },
                ["effects"] = effectsSchema
            };

            return new Dictionary<string, object>
            {
                ["type"] = "object",
                ["properties"] = properties,
                ["required"] = new List<string> { "letterTitle", "letterText", "effects" },
                ["additionalProperties"] = false
            };
        }

        private static object BuildEffectSchema(EffectConfig contract)
        {
            Dictionary<string, object> properties = new Dictionary<string, object>
            {
                ["effectName"] = new Dictionary<string, object>
                {
                    ["type"] = "string",
                    ["enum"] = new List<string> { contract.EffectName }
                }
            };
            List<string> required = new List<string> { "effectName" };

            if (contract.IsDefRequired)
            {
                List<string> allowedNames = contract.AllowedDefs
                    .Concat(contract.SecondaryAllowedDefs)
                    .Select(d => d.DefName)
                    .Distinct()
                    .ToList();

                Dictionary<string, object> itemsSchema = new Dictionary<string, object>
                {
                    ["type"] = "string"
                };
                if (allowedNames.Count > 0)
                    itemsSchema["enum"] = allowedNames;

                properties["selectedDefName"] = new Dictionary<string, object>
                {
                    ["type"] = "array",
                    ["items"] = itemsSchema
                };
                required.Add("selectedDefName");
            }

            if (contract.UsesSeverity)
            {
                properties["severity"] = new Dictionary<string, object>
                {
                    ["type"] = "string",
                };
                required.Add("severity");
            }

            if (contract.UsesTargetPawn)
            {
                properties["targetPawn"] = new Dictionary<string, object> { ["type"] = "string" };
                required.Add("targetPawn");
            }

            return new Dictionary<string, object>
            {
                ["type"] = "object",
                ["properties"] = properties,
                ["required"] = required,
                ["additionalProperties"] = false
            };
        }

        private static object BuildGenericFallbackSchema()
        {
            return new Dictionary<string, object>
            {
                ["type"] = "object",
                ["properties"] = new Dictionary<string, object>
                {
                    ["effectName"] = new Dictionary<string, object> { ["type"] = "string" }
                },
                ["required"] = new List<string> { "effectName" },
                ["additionalProperties"] = false
            };
        }
    }
}
