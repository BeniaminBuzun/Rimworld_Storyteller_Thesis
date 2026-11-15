using Newtonsoft.Json;
using RimTelligence.Memory;
using RimTelligence.Source.Memory.Storage;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Verse;

namespace RimTelligence.DataSnapshots
{
    public class ItemCategory
    {
        public string Id;
        public float Weight;
        public List<GameObjectMeta> Items = new List<GameObjectMeta>();

        /// <summary>True when this category is stored in the "items" table (listed in item-categories.json).</summary>
        public bool IsItem => RagTables.IsItemCategory(Id);
    }

    public class ItemCategoryConfigEntry
    {
        public string Id;
        public float Weight;
    }

    /// <summary>
    /// In-memory view of the categories. Category ids and weights come from item-categories.json;
    /// the def lists come ONLY from the database (see GameDefScanner / GameObjectsCapturer for how the DB is filled).
    /// </summary>
    [StaticConstructorOnStartup]
    public static class ItemCategoryRegistry
    {
        private static string ConfigPath => _configPath.Value;
        private static readonly Lazy<string> _configPath = new Lazy<string>(() =>
            ModPaths.Resolve(Path.Combine("Data", "item-categories.json")));

        public static bool IsLoaded { get; private set; }
        public static IReadOnlyDictionary<string, ItemCategory> AllCategories => _categories;

        // Replaced as a whole on every reload so readers never see a half-filled list.
        private static volatile Dictionary<string, ItemCategory> _categories = new Dictionary<string, ItemCategory>();

        static ItemCategoryRegistry()
        {
            LoadConfig();
            Reload();
        }

        public static void LoadConfig()
        {
            try
            {
                var entries = JsonConvert.DeserializeObject<List<ItemCategoryConfigEntry>>(File.ReadAllText(ConfigPath));

                // Everything in item-categories.json is an item category -> "items" table.
                RagTables.SetItemCategories(entries.Select(e => e.Id));

                var categories = new Dictionary<string, ItemCategory>();
                foreach (var entry in entries)
                    categories[entry.Id] = new ItemCategory { Id = entry.Id, Weight = entry.Weight };

                // Everything seeded here is NOT an item -> "others" table.
                SeedDefCategory(categories, "All_Animals");
                SeedDefCategory(categories, "Diseases");
                SeedDefCategory(categories, "Damage_Types");
                SeedDefCategory(categories, "Extreame_Weathers");
                SeedDefCategory(categories, "Normal_Weathers");
                SeedDefCategory(categories, "All_Skills");
                SeedDefCategory(categories, "Pawn_Body_Parts");

                _categories = categories;
                IsLoaded = true;
                ModLogger.Message("Loaded " + _categories.Count + " item categories from config.");
            }
            catch (Exception exception)
            {
                ModLogger.Message("Failed to load item categories config: " + exception.Message);
                _categories = new Dictionary<string, ItemCategory>();
                IsLoaded = false;
            }
        }

        private static void SeedDefCategory(Dictionary<string, ItemCategory> categories, string categoryId)
        {
            if (!categories.ContainsKey(categoryId))
                categories[categoryId] = new ItemCategory { Id = categoryId, Weight = 0f };
        }

        /// <summary>Kept for compatibility with older callers; same as <see cref="Reload"/>.</summary>
        public static void PopulateAll() => Reload();

        /// <summary>Rebuilds every category's list from the database. Call after the DB has been synced.</summary>
        public static void Reload()
        {
            if (!IsLoaded) return;

            try
            {
                string dbPath = ModPaths.Resolve(Path.Combine("Data", "rag.db"));
                if (!File.Exists(dbPath)) return;

                List<StaticChunk> rows = new StaticRagStore(dbPath).ReadDefs();

                var fresh = _categories.ToDictionary(
                    kv => kv.Key,
                    kv => new ItemCategory { Id = kv.Key, Weight = kv.Value.Weight });

                int skipped = 0;
                foreach (var row in rows)
                {
                    if (!fresh.TryGetValue(row.Category, out var category)) { skipped++; continue; }
                    category.Items.Add(DefMetadata.FromRow(row.DefName, row.Text, row.MetadataJson));
                }

                foreach (var category in fresh.Values)
                    category.Items.Sort((a, b) => string.Compare(a.DefName, b.DefName, StringComparison.Ordinal));

                _categories = fresh;

                if (skipped > 0)
                    ModLogger.Warning("Ignored " + skipped + " DB rows with unknown category ids (the next sync removes them).");

                if (rows.Count > 0)
                    foreach (var category in fresh.Values.Where(c => c.Items.Count == 0))
                        ModLogger.Warning("Category '" + category.Id + "' is empty in the database.");
            }
            catch (Exception ex)
            {
                ModLogger.Warning("Failed to load categories from the database: " + ex.Message);
            }
        }

        public static List<GameObjectMeta> GetList(string categoryId) =>
            (IsLoaded && categoryId != null && _categories.TryGetValue(categoryId, out var category)) ? category.Items : new List<GameObjectMeta>();

        public static List<string> GetAllowedDefs(string categoryId) =>
            GetList(categoryId).Select(m => m.DefName).ToList();

        private static readonly Random _categoryRandom = new Random();

        /// <summary>Draws a random ITEM category (weighted). Only categories stored in the items table are eligible.</summary>
        public static string DrawRandomCategory()
        {
            var itemCategories = _categories.Values.Where(c => c.IsItem && c.Items.Count > 0).ToList();

            var candidates = itemCategories.Where(c => c.Weight > 0f).ToList();
            if (candidates.Count == 0)
                candidates = itemCategories;

            if (candidates.Count == 0)
                return null; // nothing in the DB yet; callers must handle null

            float total = candidates.Sum(c => c.Weight);
            if (total <= 0f) return candidates[0].Id;

            float roll = (float)(_categoryRandom.NextDouble() * total);
            float cursor = 0f;
            foreach (var category in candidates)
            {
                cursor += category.Weight;
                if (roll <= cursor)
                    return category.Id;
            }

            return candidates.Last().Id;
        }
    }
}