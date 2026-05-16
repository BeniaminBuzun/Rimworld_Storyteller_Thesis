using System;
using System.Collections.Generic;
using System.Linq;
using RimTelligence.Source.CustomEvents.EventSelector.DTO;
using RimTelligence.Source.CustomEvents.EventSelector.Modifiers;
using RimTelligence.Source.CustomEvents.Handlers;
using RimTelligence.Source.DataSnapshots;
using Verse;

namespace RimTelligence.Source.CustomEvents.EventSelector
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
                .Select(h => new EventEntry(h.EffectName, h.BaseWeight, h.Alignment))
                .ToList();

            //Strip ineligible entries
            pool = pool.Where(e => EventEligibility.IsEligible(e.Type, snapshot)).ToList();

            //    Apply alignment filter                        
            pool = FilterByMode(pool, mode);

            if (pool.Count == 0)
            {
                ModLogger.Warning("[Selector] Pool empty after filtering, falling back to full pool.");
                pool = EffectHandlerRegistry.GetAllHandlers()
                    .Select(h => new EventEntry(h.EffectName, h.BaseWeight, h.Alignment))
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
            pool = pool.Where(e => e.Weight > 0f).ToList();

            //  Draw 
            int count = DrawCount(mode);
            var drawn = new List<EventEntry>();
            var usedFamilies = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            for (int i = 0; i < count && pool.Count > 0; i++)
            {
                EventEntry pick = WeightedDraw(pool);
                if (pick == null) break;

                drawn.Add(pick);

                // Remove exact type from pool (no duplicates)
                pool.RemoveAll(e => e.Type == pick.Type);

                // Family cap: after drawing one from a family, 
                // halve all remaining entries in that family
                // (soft cap — doesn't prevent them entirely, just makes them less likely)
                if (usedFamilies.Contains(pick.Family))
                {
                    // Second draw from same family: very heavy penalty
                    foreach (var e in pool.Where(e => e.Family == pick.Family))
                        e.Weight *= 0.1f;
                }
                else
                {
                    // First time seeing this family: mild penalty on siblings
                    foreach (var e in pool.Where(e => e.Family == pick.Family))
                        e.Weight *= 0.3f;
                }
                usedFamilies.Add(pick.Family);
            }

            var result = drawn
                .Select(e => new SelectedEventIntent { type = e.Type })
                .ToList();

            // Record for history
            history?.Record(result);

            ModLogger.Message($"[Selector] Final draw ({mode}): {string.Join(", ", result.Select(e => e.type))}");
            return result;
        }

        //  Helpers 

        private static List<EventEntry> FilterByMode(List<EventEntry> pool, DrawMode mode)
        {
            switch (mode)
            {
                case DrawMode.BadStreak:
                    return pool.Where(e => e.Alignment != EventAlignment.Good).ToList();
                case DrawMode.GoodStreak:
                    return pool.Where(e => e.Alignment != EventAlignment.Bad).ToList();
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
            for (int i = 0; i < weights.Length; i++)
            {
                cursor += weights[i];
                if (roll < cursor) return i + 1;
            }
            return 1;
        }

        private static EventEntry WeightedDraw(List<EventEntry> pool)
        {
            float total = pool.Sum(e => e.Weight);
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