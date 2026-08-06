using RimTelligence.CustomEvents.EventSelector;
using RimTelligence.DataSnapshots;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;

namespace RimTelligence.CustomEvents.EventSelector
{
    public static class DrawModeSelector
    {
        private static readonly (DrawMode mode, float weight)[] _base =
        {
            (DrawMode.Weighted,   0.55f),
            (DrawMode.PureRandom, 0.20f),
            (DrawMode.BadStreak,  0.15f),
            (DrawMode.GoodStreak, 0.10f),
        };

        public static DrawMode Pick(ColonySnapshot snapshot)
        {
            var history = EventHistory.Current;

            //  Hard overrides first 
            if (history != null)
            {
                // 4+ bad batches in a row → force relief
                if (history.ConsecutiveBadBatches >= 4)
                {
                    ModLogger.Message("[DrawMode] Override → GoodStreak (4 consecutive bad batches)");
                    return DrawMode.GoodStreak;
                }

                // 3+ good batches in a row → force some drama
                if (history.ConsecutiveGoodBatches >= 3)
                {
                    ModLogger.Message("[DrawMode] Override → BadStreak (3 consecutive good batches)");
                    return DrawMode.BadStreak;
                }
            }

            //  Weighted lottery with state nudges 
            var weights = _base.Select(entry => (entry.mode, entry.weight)).ToList();

            float avgMood = snapshot.colonists?.Count > 0
                ? snapshot.colonists.Average(colonist => colonist.needs?.mood ?? 0.5f)
                : 0.5f;

            // Colony is thriving → drama is due
            if (avgMood > 0.80f)
                Adjust(weights, DrawMode.BadStreak, +0.10f);

            // Colony is suffering → maybe ease up
            if (avgMood < 0.25f)
                Adjust(weights, DrawMode.GoodStreak, +0.08f);

            // Long peace → danger is overdue
            if (snapshot.daysSinceLastRaid > 20)
                Adjust(weights, DrawMode.BadStreak, +0.12f);

            // Recent raid → don't pile on immediately
            if (snapshot.daysSinceLastRaid <= 2)
                Adjust(weights, DrawMode.BadStreak, -0.10f);

            // Starving → reduce pure random (avoid irrelevant events)
            if (snapshot.totalNutrition < 10f)
                Adjust(weights, DrawMode.PureRandom, -0.08f);

            DrawMode result = WeightedDraw(weights);
            ModLogger.Message($"[DrawMode] Selected: {result}");
            return result;
        }

        private static void Adjust(List<(DrawMode mode, float weight)> list,
                                   DrawMode mode, float delta)
        {
            for (int index = 0; index < list.Count; index++)
                if (list[index].mode == mode)
                    list[index] = (mode, Math.Max(0f, list[index].weight + delta));
        }

        private static DrawMode WeightedDraw(List<(DrawMode mode, float weight)> list)
        {
            float total = list.Sum(entry => entry.weight);
            float roll = Rand.Value * total;
            float cursor = 0f;

            foreach (var (mode, weight) in list)
            {
                cursor += weight;
                if (roll <= cursor) return mode;
            }
            return DrawMode.Weighted;
        }
    }
}