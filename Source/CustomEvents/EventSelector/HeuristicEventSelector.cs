using RimTelligence.CustomEvents.EventSelector.Modifiers;
using RimTelligence.CustomEvents.Handlers;
using RimTelligence.DataSnapshots;
using RimTelligence.Source.CustomEvents.Handlers;
using System;
using System.Collections.Generic;
using System.Linq;
using Verse;

namespace RimTelligence.CustomEvents.EventSelector
{
    public static class HeuristicEventSelector
    {
        public static List<IEffect> Select(ColonySnapshot snapshot)
        {
            DrawMode mode = DrawModeSelector.Pick(snapshot);
            ModLogger.Message($"[Selector] DrawMode={mode}");

            //Build base pool
            var pool = new List<IEffect>();
            var weights = new Dictionary<string, float>(StringComparer.OrdinalIgnoreCase);
            var allNames = EffectFactory.GetAllEffectNames();
            foreach (var name in allNames)
            {
                IEffect effect = EffectFactory.Create(name);
                if (name == "ResourcePodCrash" || name == "PawnFoundItems")
                {
                    effect.Config.RelatedDefList = DrawRelatedDefList();
                }

                if (effect.CanBeChoosen(snapshot))
                {
                    pool.Add(effect);
                    weights[effect.Config.EffectName] = effect.Config.BaseWeight;
                }
            }

            // Apply alignment filter                        
            pool = FilterByMode(pool, mode);

            //  Apply state multipliers to local weights only
            if (mode != DrawMode.PureRandom)
            {
                foreach (var entry in pool)
                    weights[entry.Config.EffectName] *= StateModifiers.Apply(entry, snapshot);
            }

            // Strip anything that ended up at zero weight
            pool = pool.Where(entry => weights[entry.Config.EffectName] > 0f).ToList();

            //  Draw 
            int count = DrawCount();
            List<IEffect> drawn = new List<IEffect>();

            for (int index = 0; index < count && pool.Count > 0; index++)
            {
                IEffect pick = WeightedDraw(pool, weights);

                // Keep the same event drawable, but at half probability on a second draw
                weights[pick.Config.EffectName] *= 0.5f;
                drawn.Add(pick);
            }

            ModLogger.Message($"[Selector] Final draw ({mode}): {string.Join(", ", drawn.Select(entry => entry.Config.EffectName))}");
            return drawn;
        }

        //  Helpers 
        private static List<IEffect> FilterByMode(List<IEffect> pool, DrawMode mode)
        {
            switch (mode)
            {
                case DrawMode.BadStreak:
                    return pool.Where(entry => entry.Config.Alignment == EventAlignment.Bad).ToList();
                case DrawMode.GoodStreak:
                    return pool.Where(entry => entry.Config.Alignment == EventAlignment.Good).ToList();
                case DrawMode.NeutralOnly:
                    return pool.Where(entry => entry.Config.Alignment == EventAlignment.Neutral).ToList();
                default: // Weighted, PureRandom
                    return pool;
            }
        }

        private static int DrawCount()
        {
            int[] weights = { 10, 30, 35, 20, 5 }; // 1-5 events
            int total = weights.Sum();
            int roll = Rand.Range(0, total);
            int cursor = 0;
            for (int index = 0; index < weights.Length; index++)
            {
                cursor += weights[index];
                if (roll < cursor) return index + 1;
            }
            return 1;
        }

        private static IEffect WeightedDraw(List<IEffect> pool, Dictionary<string, float> weights)
        {
            float total = pool.Sum(entry => weights[entry.Config.EffectName]);
            if (total <= 0f) return null;

            float roll = Rand.Value * total;
            float cursor = 0f;

            foreach (var entry in pool)
            {
                cursor += weights[entry.Config.EffectName];
                if (roll <= cursor) return entry;
            }
            return pool.Last();
        }

        private static string DrawRelatedDefList()
        {
            return ItemCategoryRegistry.DrawRandomCategory();
        }
    }
}