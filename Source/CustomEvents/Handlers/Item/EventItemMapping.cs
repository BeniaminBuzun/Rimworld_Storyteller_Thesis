using RimTelligence.DataSnapshots;
using System.Collections.Generic;
using System.Linq;

namespace RimTelligence.CustomEvents.Handlers.Item
{
    public static class EventItemMapping
    {
        public static List<string> GetAllowedDefs(string effectType)
        {
            int underscoreIndex = effectType.IndexOf('_');
            if (underscoreIndex < 0 || underscoreIndex >= effectType.Length - 1)
                return new List<string>();

            string categoryId = effectType.Substring(underscoreIndex + 1);
            return ItemCategoryRegistry.GetAllowedDefs(categoryId);
        }
    }
}
