using RimTelligence.DataSnapshots;
using System.Linq;
using Verse;

namespace RimTelligence.CustomEvents.EventSelector
{
    public static class DrawModeSelector
    {
        private static readonly (DrawMode mode, float weight)[] _base =
        {
            (DrawMode.Weighted,    0.45f),
            (DrawMode.PureRandom,  0.20f),
            (DrawMode.BadStreak,   0.15f),
            (DrawMode.NeutralOnly, 0.10f),
            (DrawMode.GoodStreak,  0.10f),
        };

        public static DrawMode Pick(ColonySnapshot snapshot)
        {
            float total = _base.Sum(entry => entry.weight);
            float roll = Rand.Value * total;
            float cumulative = 0f;
            DrawMode chosen = _base[_base.Length - 1].mode;

            foreach (var (mode, weight) in _base)
            {
                cumulative += weight;
                if (roll < cumulative)
                {
                    chosen = mode;
                    break;
                }
            }

            ModLogger.Message($"[DrawMode] Selected: {chosen}");
            return chosen;
        }
    }
}
