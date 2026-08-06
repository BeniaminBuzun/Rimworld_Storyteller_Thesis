using System;
using System.Collections.Generic;
using System.Linq;
using RimTelligence.CustomEvents.EventSelector;
using RimTelligence.CustomEvents.EventSelector.Modifiers;
using RimTelligence.CustomEvents.Handlers;
using RimTelligence.DataSnapshots;
using Verse;

namespace RimTelligence.CustomEvents.EventSelector
{
    public static class HeuristicEventSelector
    {
        private static readonly int[] _countWeights = { 50, 30, 15, 5 }; // 1,2,3,4 events

        public static List<SelectedEventIntent> Select(ColonySnapshot snapshot, Map map)
        {
            DrawMode mode = DrawModeSelector.Pick(snapshot);
            ModLogger.Message($"[Selector] DrawMode={mode}");

            //Build base pool from registry
            var pool = EffectHandlerRegistry.GetAllHandlers()
                .Select(handler => new EventEntry(handler.EffectName, handler.BaseWeight, handler.Alignment))
                .ToList();

            //Strip ineligible entries
            pool = pool.Where(entry => EventEligibility.IsEligible(entry.Type, snapshot)).ToList();

            //    Apply alignment filter                        
            pool = FilterByMode(pool, mode);

            if (pool.Count == 0)
            {
                ModLogger.Warning("[Selector] Pool empty after filtering, falling back to full pool.");
                pool = EffectHandlerRegistry.GetAllHandlers()
                    .Select(handler => new EventEntry(handler.EffectName, handler.BaseWeight, handler.Alignment))
                    .ToList();
            }

            //  Apply state multipliers (skip in PureRandom) 
            if (mode != DrawMode.PureRandom)
            {
                foreach (var entry in pool)
                    entry.Weight *= StateModifiers.Apply(entry, snapshot);
            }

            //  Apply recency penalties 
            var history = EventHistory.Current;
            if (history != null)
            {
                foreach (var entry in pool)
                {
                    // Per-type penalty: full weight reduction for recently seen types
                    float typePenalty = history.RecencyPenalty(entry.Type);
                    // Per-family penalty: softer, prevents e.g. 3 pod crashes in a row
                    float familyPenalty = history.FamilyRecencyPenalty(entry.Family) * 0.5f;

                    float penalty = Math.Max(typePenalty, familyPenalty);
                    entry.Weight *= (1f - penalty);
                }
            }

            // Strip anything that ended up at zero weight
            pool = pool.Where(entry => entry.Weight > 0f).ToList();

            //  Draw 
            int count = DrawCount(mode);
            var drawn = new List<EventEntry>();
            var usedFamilies = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            for (int index = 0; index < count && pool.Count > 0; index++)
            {
                EventEntry pick = WeightedDraw(pool);
                if (pick == null) break;

                drawn.Add(pick);

                // Remove exact type from pool (no duplicates)
                pool.RemoveAll(entry => entry.Type == pick.Type);

                // Family cap: after drawing one from a family, 
                // halve all remaining entries in that family
                // (soft cap — doesn't prevent them entirely, just makes them less likely)
                if (usedFamilies.Contains(pick.Family))
                {
                    // Second draw from same family: very heavy penalty
                    foreach (var entry in pool.Where(entry => entry.Family == pick.Family))
                        entry.Weight *= 0.1f;
                }
                else
                {
                    // First time seeing this family: mild penalty on siblings
                    foreach (var entry in pool.Where(entry => entry.Family == pick.Family))
                        entry.Weight *= 0.3f;
                }
                usedFamilies.Add(pick.Family);
            }

            var result = drawn
                .Select(entry => new SelectedEventIntent { type = entry.Type })
                .ToList();

            // Record for history
            history?.Record(result);

            ModLogger.Message($"[Selector] Final draw ({mode}): {string.Join(", ", result.Select(entry => entry.type))}");
            return result;
        }

        //  Helpers 

        private static List<EventEntry> FilterByMode(List<EventEntry> pool, DrawMode mode)
        {
            switch (mode)
            {
                case DrawMode.BadStreak:
                    return pool.Where(entry => entry.Alignment != EventAlignment.Good).ToList();
                case DrawMode.GoodStreak:
                    return pool.Where(entry => entry.Alignment != EventAlignment.Bad).ToList();
                default: // Weighted, PureRandom
                    return pool;
            }
        }

        private static int DrawCount(DrawMode mode)
        {
            switch (mode)
            {
                case DrawMode.PureRandom:
                    // Genuinely flat 1-4
                    return Rand.RangeInclusive(1, 4);

                case DrawMode.BadStreak:
                    // Can pile on, but weight toward 1-2
                    return WeightedCountDraw(new[] { 40, 35, 20, 5 });

                case DrawMode.GoodStreak:
                    // Good things come in smaller doses
                    return WeightedCountDraw(new[] { 60, 30, 8, 2 });

                default: // Weighted
                    return WeightedCountDraw(_countWeights);
            }
        }

        private static int WeightedCountDraw(int[] weights)
        {
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

        private static EventEntry WeightedDraw(List<EventEntry> pool)
        {
            float total = pool.Sum(entry => entry.Weight);
            if (total <= 0f) return null;

            float roll = Rand.Value * total;
            float cursor = 0f;

            foreach (var entry in pool)
            {
                cursor += entry.Weight;
                if (roll <= cursor) return entry;
            }
            return pool.Last();
        }
    }
}