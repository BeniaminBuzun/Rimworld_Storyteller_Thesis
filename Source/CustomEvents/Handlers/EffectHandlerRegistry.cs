using RimTelligence.Source.CustomEvents.Handlers.Implementations;
using RimTelligence.Source.CustomEvents.Handlers.Implementations.Environment;
using RimTelligence.Source.CustomEvents.Handlers.Implementations.ItemsRelated;
using RimTelligence.Source.CustomEvents.Handlers.Implementations.Pawns;
using RimTelligence.Source.CustomEvents.Handlers.Implementations.Pawns.Debuffs;
using System;
using System.Collections.Generic;
using System.Linq;

namespace RimTelligence.Source.CustomEvents.Handlers
{
    public static class EffectHandlerRegistry
    {
        private static readonly Dictionary<string, IEffectHandler> Handlers = new Dictionary<string, IEffectHandler>(StringComparer.OrdinalIgnoreCase);

        static EffectHandlerRegistry()
        {
            Register(new DamagePawnHandler());
            Register(new StartExtremeWeatherEvent());
            Register(new AnimalInsanityHandler());
            Register(new SelfTameHandler());
            Register(new AlphabeaversHandler());
            Register(new AddDiseaseHandler());
            Register(new PawnFoundItemsHandler());
            Register(new MiracleHealChronicConditionHandler());
            Register(new MiracleHealDiseaseHandler());
            Register(new MiracleHealMissingPartHandler());
            Register(new ResourcePodCrashHandler());
            Register(new ChangeWeatherHandler());
            Register(new MeteoriteImpactHandler());
            Register(new AddExpPointsHandler());
            Register(new LoseExpPointsHandler());
            Register(new CropBlightHandler());
            Register(new MoodBuffHandler());
            Register(new MoodDebuffHandler());
            Register(new LoseItemsHandler());
            Register(new SpawnItemsAtRandomLocationHandler());
        }

        private static void Register(IEffectHandler handler)
        {
            Handlers[handler.EffectType] = handler;
        }

        public static IEffectHandler GetHandler(string effectType)
        {
            Handlers.TryGetValue(effectType, out var handler);
            return handler;
        }

        public static IEnumerable<IEffectHandler> GetAllHandlers() => Handlers.Values;
    }
}