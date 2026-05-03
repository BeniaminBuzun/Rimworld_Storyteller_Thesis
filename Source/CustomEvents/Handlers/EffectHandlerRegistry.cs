using RimTelligence.Source.CustomEvents.Handlers.Implementations;
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
            Register(new SpawnPawnsHandler()); // also needs to be rebuild 
            Register(new DropAtRandomPlaceItemsHandler());
            Register(new DamagePawnHandler());
            //Register(new StartFireHandler()); // disabled for now 
            Register(new FireConditionHandler());
            //Register(new SpawnJoinableHandler()); // disabled for now 
            Register(new AnimalInsanityHandler());
            Register(new SelfTameHandler());
            Register(new AlphabeaversHandler());
            Register(new DiseaseHandler());
            Register(new PawnFoundItemsHandler());
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