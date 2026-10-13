using RimTelligence.CustomEvents.EventSelector.Modifiers;
using RimTelligence.CustomEvents.Handlers;
using RimTelligence.DataSnapshots;
using RimTelligence.Source.CustomEvents.Handlers;
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
            var allNames = EffectFactory.GetAllEffectNames();
            foreach (var name in allNames)
            {
                IEffect effect = EffectFactory.Create(name);
                if (name == "ResourcePodCrash" || name == "PawnFoundItems")
                {
                    string selectedRelatedDefList = DrawRelatetDefList();
                    effect.Config.RelatedDefList = selectedRelatedDefList;
                }

                if (effect.CanBeChoosen(snapshot))
                {
                    pool.Add(effect);
                }
            }

            // Apply alignment filter                        
            pool = FilterByMode(pool, mode);

            //  Apply state multipliers 
            if (mode != DrawMode.PureRandom)
            {
                foreach (var entry in pool)
                    entry.Config.BaseWeight *= StateModifiers.Apply(entry, snapshot);
            }

            // Strip anything that ended up at zero weight
            pool = pool.Where(entry => entry.Config.BaseWeight > 0f).ToList();

            //  Draw 
            int count = DrawCount();
            List<IEffect> drawn = new List<IEffect>();

            for (int index = 0; index < count && pool.Count > 0; index++)
            {
                IEffect pick = WeightedDraw(pool);

                // Keep the same event drawable, but at half probability on a second draw
                pick.Config.BaseWeight *= 0.01f;
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

        private static IEffect WeightedDraw(List<IEffect> pool)
        {
            float total = pool.Sum(entry => entry.Config.BaseWeight);
            if (total <= 0f) return null;

            float roll = Rand.Value * total;
            float cursor = 0f;

            foreach (var entry in pool)
            {
                cursor += entry.Config.BaseWeight;
                if (roll <= cursor) return entry;
            }
            return pool.Last();
        }

        private static string DrawRelatetDefList()
        {
            List<ItemCategory> candidates = ItemCategoryRegistry.AllCategories.Values
                .ToList();

            float total = candidates.Sum(category => category.Weight);
            float roll = Rand.Value * total;
            float cursor = 0f;

            foreach (ItemCategory category in candidates)
            {
                cursor += category.Weight;
                if (roll <= cursor)
                    return category.Id;
            }

            return candidates.Last().Id;
        }
    }
}