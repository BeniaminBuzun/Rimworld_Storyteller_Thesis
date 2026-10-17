using RimTelligence.CustomEvents.Handlers;
using RimTelligence.CustomEvents.Handlers.Animal;
using RimTelligence.CustomEvents.Handlers.Environment;
using RimTelligence.CustomEvents.Handlers.Item;
using RimTelligence.CustomEvents.Handlers.Pawns.Buffs;
using RimTelligence.CustomEvents.Handlers.Pawns.Debuffs;
using RimTelligence.CustomEvents.Handlers.Pawns.Miracles;
using RimTelligence.Source.CustomEvents.Handlers.Pawns.Buffs;
using RimTelligence.Source.CustomEvents.Handlers.Pawns.Debuffs;
using System;
using System.Collections.Generic;

namespace RimTelligence.Source.CustomEvents.Handlers
{
    public static class EffectFactory
    {
        private static readonly Dictionary<string, Func<IEffect>> Registry =
            new Dictionary<string, Func<IEffect>>(StringComparer.OrdinalIgnoreCase);

        static EffectFactory()
        {
            Register<AnimalInsanity>("AnimalInsanity");
            Register<SelfTame>("SelfTame");
            Register<SpawnAnimals>("SpawnAnimals");
            Register<CropBlight>("CropBlight");
            Register<AddDisease>("AddDisease");
            Register<MiracleHealChronicCondition>("MiracleHealChronicCondition");
            Register<MiracleHealDisease>("MiracleHealDisease");
            Register<MiracleHealMissingPart>("MiracleHealMissingPart");
            Register<DamagePawn>("DamagePawn");
            Register<StartExtremeWeather>("StartExtremeWeatherHandler");
            Register<ChangeWeather>("ChangeWeather");
            Register<MeteoriteImpact>("MeteoriteImpact");
            Register<AddExpPoints>("AddExpPoints");
            Register<LoseExpPoints>("LoseExpPoints");
            Register<MoodBuff>("MoodBuff");
            Register<MoodDebuff>("MoodDebuff");
            Register<LoseItems>("LoseItems");
            Register<PawnFoundItems>("PawnFoundItems");
            Register<ResourcePodCrash>("ResourcePodCrash");
        }

        private static void Register<T>(string effectName) where T : IEffect, new()
        {
            Registry[effectName] = () => new T();
        }

        public static IEffect Create(string effectName)
        {

            if (Registry.TryGetValue(effectName, out var factory))
                return factory();
            return null;
        }

        public static IEnumerable<string> GetAllEffectNames()
        {
            return Registry.Keys;
        }
    }
}
