using RimTelligence.CustomEvents.Handlers.Item;
using RimWorld.Planet;
using System.Collections.Generic;
using System.Linq;
using Verse;

namespace RimTelligence.CustomEvents.Handlers.Item
{
    public abstract class GenericItemHandler : IEffectHandler
    {
        public abstract string EffectName { get; }

        public abstract float BaseWeight { get; }

        public abstract EventAlignment Alignment { get; }

        public abstract string NarrativeDescription { get; }
        public abstract string PromptStructure { get; }

        public string AditionalPromptStructureInfo =>
            string.Join(", ", EventItemMapping.GetAllowedDefs(EffectName));

        public abstract GlobalTargetInfo Execute(GameEffect effect, Map map);
        public abstract string GetSummary(GameEffect effect);

        protected ThingDef ValidateDef(GameEffect effect)
        {
            var allowed = EventItemMapping.GetAllowedDefs(EffectName);
            if (!allowed.Contains(effect.defName))
            {
                ModLogger.Warning($"{EffectName}: '{effect.defName}' not in allowed list for this category.");
                return null;
            }
            ThingDef def = DefDatabase<ThingDef>.GetNamed(effect.defName, errorOnFail: false);
            if (def == null)
                ModLogger.Warning($"{EffectName}: ThingDef '{effect.defName}' not found in DefDatabase.");
            return def;
        }
    }
}