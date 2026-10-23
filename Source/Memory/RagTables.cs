using System;
using System.Collections.Generic;
using System.Linq;

namespace RimTelligence.Memory
{
    /// <summary>
    /// Single source of truth for which SQLite table a category lives in.
    /// Every category listed in item-categories.json is an item; everything else is "others".
    /// </summary>
    public static class RagTables
    {
        public const string Items = "items";
        public const string Others = "others";
        public static readonly string[] All = { Items, Others };

        private static readonly HashSet<string> _itemIds = new HashSet<string>(StringComparer.Ordinal);
        private static readonly object _lock = new object();

        /// <summary>Called by ItemCategoryRegistry.LoadConfig with the ids from item-categories.json.</summary>
        public static void SetItemCategories(IEnumerable<string> ids)
        {
            lock (_lock)
            {
                _itemIds.Clear();
                foreach (var id in ids)
                    if (!string.IsNullOrEmpty(id)) _itemIds.Add(id);
            }
        }

        public static string[] ItemCategoryIds
        {
            get { lock (_lock) return _itemIds.ToArray(); }
        }

        public static bool IsItemCategory(string id)
        {
            lock (_lock) return id != null && _itemIds.Contains(id);
        }

        public static string For(string id) => IsItemCategory(id) ? Items : Others;
    }
}