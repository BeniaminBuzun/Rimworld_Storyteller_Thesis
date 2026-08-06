using RimTelligence.CustomEvents;
using RimWorld;
using System;
using Verse;

namespace RimTelligence.CustomEvents.Handlers.Item
{
    public static class WealthBasedQuantityResolver
    {
        private const float LowMin = 0.005f;
        private const float LowMax = 0.015f;

        private const float MediumMin = 0.016f;
        private const float MediumMax = 0.026f;

        private const float HighMin = 0.03f;
        private const float HighMax = 0.06f;

        private const float FallbackWealth = 10_000f;

        public static int Resolve(ThingDef def, SeverityTypes value, Map map, int fallbackCount = 1)
        {
            if (value == SeverityTypes.None)
                return Math.Max(fallbackCount, 1);

            float wealth = GetColonyWealth(map);
            float budget = wealth * PickFraction(value);
            float price = def.BaseMarketValue;

            // Guard against free / zero-value items producing absurd quantities.
            if (price <= 0f)
                price = 1f;

            int quantity = (int)Math.Round(budget / price);
            quantity = Math.Max(quantity, 1);
            quantity = Math.Min(quantity, def.stackLimit > 0 ? def.stackLimit : quantity);

            return quantity;
        }

        //
        //
        //
        //
        //
        //
        //
        //
        //
        //  Private helpers 

        private static float GetColonyWealth(Map map)
        {
            if (map?.wealthWatcher == null)
                return FallbackWealth;

            float total = map.wealthWatcher.WealthTotal;
            return total > 0f ? total : FallbackWealth;
        }

        private static float PickFraction(SeverityTypes value)
        {
            switch (value)
            {
                case SeverityTypes.Low: return Rand.Range(LowMin, LowMax);
                case SeverityTypes.Medium: return Rand.Range(MediumMin, MediumMax);
                case SeverityTypes.High: return Rand.Range(HighMin, HighMax);
                default: return LowMin;
            }
        }
    }
}