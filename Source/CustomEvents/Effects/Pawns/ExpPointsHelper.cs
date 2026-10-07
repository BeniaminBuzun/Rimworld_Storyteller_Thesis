using RimTelligence.CustomEvents;
using System;

namespace RimTelligence.Source.CustomEvents.Handlers.Pawns
{
    internal class ExpPointsHelper
    {
        private static int GetXpToNextLevel(int level)
        {
            int[] xpTable =
            {
                1000,  2000,  3000,  4000,  5000,
                6000,  7000,  8000,  9000,  10000,
                12000, 14000, 16000, 18000, 20000,
                22000, 24000, 26000, 28000, 30000,
                32000
            };
            return xpTable[Math.Min(level, xpTable.Length - 1)];
        }

        public static int GetXpReward(int level, SeverityTypes value)
        {
            int xpToNext = GetXpToNextLevel(level);

            float factor;
            switch (value)
            {
                case SeverityTypes.Extreme:
                    factor = 0.8f;
                    break;
                case SeverityTypes.High:
                    factor = 0.6f;
                    break;
                case SeverityTypes.Medium:
                    factor = 0.3f;
                    break;
                default:
                    factor = 0.15f;
                    break;
            }

            return (int)(xpToNext * factor);
        }
    }
}
