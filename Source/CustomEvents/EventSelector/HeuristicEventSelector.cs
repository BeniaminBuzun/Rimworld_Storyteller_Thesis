using System;
using System.Collections.Generic;
using System.Linq;
using RimTelligence.CustomEvents.EventSelector.Modifiers;
using RimTelligence.CustomEvents.Handlers;
using RimTelligence.DataSnapshots;
using Verse;

namespace RimTelligence.CustomEvents.EventSelector
{
    public static class HeuristicEventSelector
    {
        public static List<SelectedEventIntent> Select(ColonySnapshot snapshot)
        {
            DrawMode mode = DrawModeSelector.Pick(snapshot);
            ModLogger.Message($"[Selector] DrawMode={mode}");

            //Build base pool from registry
            var pool = EffectHandlerRegistry.GetAllHandlers()
                .Select(handler => new EventEntry(handler.EffectName, handler.BaseWeight, handler.Alignment))
                .ToList();

            //    Apply alignment filter                        
            pool = FilterByMode(pool, mode);

            //Strip ineligible entries
            pool = pool.Where(entry => EventEligibility.IsEligible(entry.Type, snapshot)).ToList();

            //  Apply state multipliers 
            if (mode != DrawMode.PureRandom)
            {
                foreach (var entry in pool)
                    entry.Weight *= StateModifiers.Apply(entry, snapshot);
            }

            //  History (recorded after the draw)
            var history = EventHistory.Current;

            // Strip anything that ended up at zero weight
            pool = pool.Where(entry => entry.Weight > 0f).ToList();

            //  Draw 
            int count = DrawCount();
            var drawn = new List<EventEntry>();

            for (int index = 0; index < count && pool.Count > 0; index++)
            {
                EventEntry pick = WeightedDraw(pool);
                if (pick == null) break;

                drawn.Add(pick);

                // Keep the same event drawable, but at half probability on a second draw
                pick.Weight *= 0.5f;
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
                case DrawMode.NeutralOnly:
                    return pool.Where(entry => entry.Alignment == EventAlignment.Neutral).ToList();
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