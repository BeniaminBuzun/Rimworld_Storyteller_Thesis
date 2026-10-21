using System;
using Verse;

namespace RimTelligence.CustomEvents.Handlers.Item
{
    public static class ItemHandlerSupport
    {
        private const float LowMin = 0.005f, LowMax = 0.015f;
        private const float MediumMin = 0.016f, MediumMax = 0.026f;
        private const float HighMin = 0.03f, HighMax = 0.06f;
        private const float FallbackWealth = 10_000f;

        public static int ResolveQuantity(ThingDef def, SeverityTypes value, Map map)
        {
            if (value == SeverityTypes.None)
                return 1;

            float wealth = GetColonyWealth(map);
            float budget = wealth * PickFraction(value);
            float price = def.BaseMarketValue > 0f ? def.BaseMarketValue : 1f;

            int quantity = (int)Math.Round(budget / price);
            quantity = Math.Max(quantity, 1);
            quantity = Math.Min(quantity, def.stackLimit > 0 ? def.stackLimit : quantity);

            return quantity;
        }

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