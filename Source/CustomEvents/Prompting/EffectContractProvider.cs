using RimTelligence.CustomEvents.Handlers;
using RimTelligence.DataSnapshots;
using System.Collections.Generic;
using System.Linq;

namespace RimTelligence.CustomEvents.Prompting
{
    public static class EventJsonSchemaBuilder
    {
        public static object Build(List<IEffect> chosenEvents, ColonySnapshot snapshot)
        {
            List<object> effectSchemas = chosenEvents
                .Select(effect => BuildEffectSchema(EffectContractProvider.GetContract(effect, snapshot)))
                .Cast<object>()
                .ToList();

            Dictionary<string, object> effectsSchema = new Dictionary<string, object>
            {
                ["type"] = "array",
                ["minItems"] = 1,
                ["items"] = new Dictionary<string, object> { ["anyOf"] = effectSchemas }
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
                    ["items"] = itemsSchema,
                    ["minItems"] = contract.DefCount,
                    ["maxItems"] = contract.DefCount
                };
                required.Add("selectedDefName");
            }

            if (contract.UsesSeverity)
            {
                properties["severity"] = new Dictionary<string, object>
                {
                    ["type"] = "string",
                    ["enum"] = new List<string> { "LOW", "MEDIUM", "HIGH", "EXTREME" }
                };
            }

            if (contract.UsesTargetPawn)
            {
                properties["targetPawn"] = new Dictionary<string, object> { ["type"] = "string" };
            }

            return new Dictionary<string, object>
            {
                ["type"] = "object",
                ["properties"] = properties,
                ["required"] = required,
                ["additionalProperties"] = false
            };
        }
    }
}