using Newtonsoft.Json;
using RimTelligence.DataSnapshots;
using RimWorld.Planet;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Verse;

namespace RimTelligence.CustomEvents.Handlers
{

    [StaticConstructorOnStartup]
    public static class EffectConfigRegistry
    {
        private static string ConfigPath => new Lazy<string>(() =>
            ModPaths.Resolve(Path.Combine("Data", "effect_config.json"))).Value;

        private static readonly Dictionary<string, EffectConfig> Configs =
            new Dictionary<string, EffectConfig>(StringComparer.OrdinalIgnoreCase);

        static EffectConfigRegistry()
        {
            LoadConfig();
        }

        public static void LoadConfig()
        {
            try
            {
                if (!File.Exists(ConfigPath))
                {
                    ModLogger.Error("Handlers config not found at " + ConfigPath +
                        ". No handler configs registered.");
                    return;
                }

                var file = JsonConvert.DeserializeObject<List<EffectConfig>>(File.ReadAllText(ConfigPath));

                if (file?.Count == 0)
                {
                    ModLogger.Error("Handlers config is empty or invalid. No handler configs registered.");
                    return;
                }

                foreach (var entry in file)
                {
                    Configs[entry.EffectName] = entry;
                }

                ModLogger.Message("Loaded " + Configs.Count + " handler configs.");
            }
            catch (Exception exception)
            {
                ModLogger.Error("Failed to load handlers config: " + exception.Message);
                Configs.Clear();
            }
        }

        public static EffectConfig Get(string EffectName)
        {
            if (Configs.TryGetValue(EffectName, out EffectConfig config))
                return config;
            throw new InvalidOperationException(
                $"No handler config registered for EffectName '{EffectName}'. Check Data/handlers_config.json.");
        }

        public static EffectConfig Register(EffectConfig config)
        {
            if (config == null)
                throw new ArgumentNullException(nameof(config));
            Configs[config.EffectName] = config;
            return config;
        }
    }
}
