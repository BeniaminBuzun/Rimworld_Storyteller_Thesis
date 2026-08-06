using RimTelligence.CustomEvents.Handlers.Animal;
using RimTelligence.CustomEvents.Handlers.Environment;
using RimTelligence.CustomEvents.Handlers.Item;
using RimTelligence.CustomEvents.Handlers.Pawns.Buffs;
using RimTelligence.CustomEvents.Handlers.Pawns.Debuffs;
using RimTelligence.CustomEvents.Handlers.Pawns.Miracles;
using RimTelligence.DataSnapshots;
using System;
using System.Collections.Generic;

namespace RimTelligence.CustomEvents.Handlers
{
    public static class EffectHandlerRegistry
    {
        private static readonly Dictionary<string, IEffectHandler> Handlers =
            new Dictionary<string, IEffectHandler>(StringComparer.OrdinalIgnoreCase);

        static EffectHandlerRegistry()
        {
            foreach (var kvp in ItemCategoryRegistry.AllCategories)
            {
                string categoryId = kvp.Key;
                var category = kvp.Value;

                foreach (string variantKey in ItemCategoryRegistry.AllVariantKeys)
                {
                    var variant = ItemCategoryRegistry.GetVariant(categoryId, variantKey);
                    if (variant == null) continue;

                    string prefix = ItemCategoryRegistry.GetVariantPrefix(variantKey);
                    if (string.IsNullOrEmpty(prefix)) continue;

                    string type = prefix + "_" + categoryId;

                    switch (prefix)
                    {
                        case "ResourcePodCrash":
                            Register(new ResourcePodCrashHandler(type, variant.Description, variant.Weight, variant.Alignment));
                            break;
                        case "PawnFoundItem":
                            Register(new PawnFoundItemsHandler(type, variant.Description, variant.Weight, variant.Alignment));
                            break;
                        case "LoseItems":
                            Register(new LoseItemsHandler(type, variant.Description, variant.Weight, variant.Alignment));
                            break;
                    }
                }
            }

            Register(new DamagePawnHandler());
            Register(new StartExtremeWeatherEvent());
            Register(new AnimalInsanityHandler());
            Register(new SelfTameHandler());
            Register(new SpawnAnimalsHandler());
            Register(new AddDiseaseHandler());
            Register(new MiracleHealChronicConditionHandler());
            Register(new MiracleHealDiseaseHandler());
            Register(new MiracleHealMissingPartHandler());
            Register(new ChangeWeatherHandler());
            Register(new MeteoriteImpactHandler());
            Register(new AddExpPointsHandler());
            Register(new LoseExpPointsHandler());
            Register(new CropBlightHandler());
            Register(new MoodBuffHandler());
            Register(new MoodDebuffHandler());
        }

        private static void Register(IEffectHandler handler)
        {
            Handlers[handler.EffectName] = handler;
        }

        public static IEffectHandler GetHandler(string effectType)
        {
            Handlers.TryGetValue(effectType, out var handler);
            return handler;
        }

        public static IEnumerable<IEffectHandler> GetAllHandlers()
        {
            return Handlers.Values;
        }
    }
}
