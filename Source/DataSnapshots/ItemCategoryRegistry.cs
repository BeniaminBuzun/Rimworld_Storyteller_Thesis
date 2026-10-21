using Newtonsoft.Json;
using RimWorld;
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
    }

    public class ItemCategoryConfigEntry
    {
        public string Id;
        public float Weight;
    }

    [StaticConstructorOnStartup]
    public static class ItemCategoryRegistry
    {
        private static string ConfigPath => _configPath.Value;
        private static readonly Lazy<string> _configPath = new Lazy<string>(() =>
            ModPaths.Resolve(Path.Combine("Data", "item-categories.json")));

        public static bool IsLoaded { get; private set; }
        public static IReadOnlyDictionary<string, ItemCategory> AllCategories => _categories;

        private static Dictionary<string, ItemCategory> _categories = new Dictionary<string, ItemCategory>();

        static ItemCategoryRegistry()
        {
            LoadConfig();
            PopulateAll();
        }

        public static void LoadConfig()
        {
            try
            {
                var entries = JsonConvert.DeserializeObject<List<ItemCategoryConfigEntry>>(File.ReadAllText(ConfigPath));

                var categories = new Dictionary<string, ItemCategory>();
                foreach (var entry in entries)
                {
                    categories[entry.Id] = new ItemCategory { Id = entry.Id, Weight = entry.Weight };
                }

                SeedDefCategory(categories, "AnimalList");
                SeedDefCategory(categories, "DiseaseList");
                SeedDefCategory(categories, "DamageTypeList");
                SeedDefCategory(categories, "GameConditionList");
                SeedDefCategory(categories, "AllWeathersList");
                SeedDefCategory(categories, "AllSkillsList");
                SeedDefCategory(categories, "BodyPartList");
                SeedDefCategory(categories, "CharvestablePlantsList");

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

        public static void PopulateAll()
        {
            foreach (var category in _categories.Values)
                category.Items.Clear();

            bool hasGameDefs = false;
            try
            {
                hasGameDefs = DefDatabase<ThingDef>.AllDefs != null && DefDatabase<ThingDef>.AllDefs.Any();
            }
            catch { }

            if (hasGameDefs)
            {
                PopulateFromGameDefs();
            }
            else
            {
                PopulateFromFallbackStore();
            }

            EnsureDefaultLists();

            foreach (var category in _categories.Values)
                category.Items.Sort((a, b) => string.Compare(a.DefName, b.DefName, StringComparison.Ordinal));
        }

        private static void PopulateFromGameDefs()
        {
            var itemPredicates = BuildItemPredicates();

            foreach (ThingDef thingDef in DefDatabase<ThingDef>.AllDefs)
            {
                if (thingDef.IsBlueprint || thingDef.IsFrame || thingDef.isUnfinishedThing) continue;

                if (thingDef.IsCorpse) { TryAdd("Animal_Corpses", thingDef); continue; }

                if (thingDef.category == ThingCategory.Pawn)
                {
                    if (thingDef.race?.Animal == true) TryAdd("AnimalList", thingDef);
                    continue;
                }

                if (thingDef.category == ThingCategory.Plant)
                {
                    if (thingDef.plant?.harvestYield > 0) TryAdd("CharvestablePlantsList", thingDef);
                    continue;
                }

                if (thingDef.category != ThingCategory.Item) continue;

                foreach (var pred in itemPredicates)
                {
                    if (pred.predicate(thingDef)) { TryAdd(pred.id, thingDef); break; }
                }
            }

            TryAddDefs("DiseaseList", DefDatabase<HediffDef>.AllDefs,
                h => !(h.hediffClass != null && typeof(Hediff_Injury).IsAssignableFrom(h.hediffClass)));
            TryAddDefs("DamageTypeList", DefDatabase<DamageDef>.AllDefs, null);
            TryAddDefs("GameConditionList", DefDatabase<GameConditionDef>.AllDefs, null);
            TryAddDefs("AllWeathersList", DefDatabase<WeatherDef>.AllDefs, null);
            TryAddDefs("AllSkillsList", DefDatabase<SkillDef>.AllDefs, null);
            TryAddDefs("BodyPartList", DefDatabase<BodyPartDef>.AllDefs, null);
        }

        private static void PopulateFromFallbackStore()
        {
            try
            {
                string dbPath = ModPaths.Resolve(Path.Combine("Data", "rag.db"));
                if (!File.Exists(dbPath)) return;

                using (var conn = new System.Data.SQLite.SQLiteConnection("Data Source=" + dbPath + ";Version=3;"))
                {
                    conn.Open();
                    using (var cmd = conn.CreateCommand())
                    {
                        cmd.CommandText = "SELECT category, def_name, text FROM static_chunks";
                        using (var reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                string catId = reader.GetString(0);
                                string defName = reader.GetString(1);
                                string text = reader.GetString(2);

                                if (!_categories.TryGetValue(catId, out var category))
                                {
                                    category = new ItemCategory { Id = catId, Weight = 1.0f };
                                    _categories[catId] = category;
                                }

                                string label = defName;
                                string description = string.Empty;
                                if (!string.IsNullOrEmpty(text))
                                {
                                    int colonIdx = text.IndexOf(':');
                                    if (colonIdx > 0)
                                    {
                                        label = text.Substring(0, colonIdx).Trim();
                                        description = text.Substring(colonIdx + 1).Trim();
                                    }
                                    else
                                    {
                                        label = text.Trim();
                                    }
                                }

                                category.Items.Add(new GameObjectMeta
                                {
                                    DefName = defName,
                                    Label = label,
                                    Description = description
                                });
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                ModLogger.Warning("Failed to populate categories from fallback store: " + ex.Message);
            }
        }

        private static readonly (string defName, string label)[] StandardBodyParts = new[]
        {
            ("Torso", "torso"),
            ("Head", "head"),
            ("Eye", "eye"),
            ("Ear", "ear"),
            ("Nose", "nose"),
            ("Jaw", "jaw"),
            ("Neck", "neck"),
            ("Shoulder", "shoulder"),
            ("Arm", "arm"),
            ("Hand", "hand"),
            ("Finger", "finger"),
            ("Spine", "spine"),
            ("Rib", "rib"),
            ("Leg", "leg"),
            ("Foot", "foot"),
            ("Toe", "toe"),
            ("Heart", "heart"),
            ("Lung", "lung"),
            ("Kidney", "kidney"),
            ("Liver", "liver"),
            ("Stomach", "stomach"),
            ("Brain", "brain")
        };

        private static readonly (string defName, string label)[] StandardPlants = new[]
        {
            ("Plant_Rice", "rice plant"),
            ("Plant_Potato", "potato plant"),
            ("Plant_Corn", "corn plant"),
            ("Plant_Healroot", "healroot"),
            ("Plant_Smokeleaf", "smokeleaf"),
            ("Plant_Psychoid", "psychoid plant"),
            ("Plant_Cotton", "cotton plant"),
            ("Plant_Haygrass", "haygrass"),
            ("Plant_Devilstrand", "devilstrand")
        };

        private static void EnsureDefaultLists()
        {
            if (_categories.TryGetValue("BodyPartList", out var bodyParts) && bodyParts.Items.Count == 0)
            {
                foreach (var (defName, label) in StandardBodyParts)
                {
                    bodyParts.Items.Add(new GameObjectMeta
                    {
                        DefName = defName,
                        Label = label,
                        Description = label
                    });
                }
            }

            if (_categories.TryGetValue("CharvestablePlantsList", out var plants) && plants.Items.Count == 0)
            {
                foreach (var (defName, label) in StandardPlants)
                {
                    plants.Items.Add(new GameObjectMeta
                    {
                        DefName = defName,
                        Label = label,
                        Description = label
                    });
                }
            }
        }

        private static void TryAdd(string categoryId, ThingDef thingDef)
        {
            if (_categories.TryGetValue(categoryId, out var category))
                category.Items.Add(MetaFromThing(thingDef));
        }

        private static void TryAddDefs<TDef>(string categoryId, IEnumerable<TDef> defs, Func<TDef, bool> filter) where TDef : Def
        {
            if (!_categories.TryGetValue(categoryId, out var category)) return;
            foreach (var def in defs)
            {
                if (filter != null && !filter(def)) continue;
                category.Items.Add(MetaFromDef(def));
            }
        }

        private static List<(string id, Func<ThingDef, bool> predicate)> BuildItemPredicates()
        {
            var list = new List<(string, Func<ThingDef, bool>)>();
            void Add(string id, Func<ThingDef, bool> predicate) => list.Add((id, predicate));

            Add("WeaponsTribal", t => t.IsWeapon && (t.techLevel == TechLevel.Neolithic || t.techLevel == TechLevel.Medieval));
            Add("WeaponsIndustrial", t => t.IsWeapon && t.techLevel == TechLevel.Industrial);
            Add("WeaponsSpacer", t => t.IsWeapon && t.techLevel == TechLevel.Spacer);
            Add("WeaponsArchotech", t => t.IsWeapon && t.techLevel == TechLevel.Archotech);
            Add("WeaponsUltra", t => t.IsWeapon && t.techLevel == TechLevel.Ultra);
            Add("WeaponsOther", t => t.IsWeapon);

            Add("DrugsList", t => t.IsIngestible && t.IsDrug);
            Add("MedicineList", t => t.IsMedicine);
            Add("Apparel_Clothing", t => t.IsApparel);

            bool PlainFood(ThingDef t) => t.IsIngestible && !t.IsDrug && !t.IsMedicine;

            Add("Food_Meals", t => PlainFood(t) && HasCategory(t, "FoodMeals"));
            Add("Food_RawPlant", t => PlainFood(t) && (HasCategory(t, "PlantFoodRaw") || HasCategory(t, "PlantMatter")));
            Add("Food_RawMeat", t => PlainFood(t) && HasCategory(t, "MeatRaw"));
            Add("Food_AnimalProducts", t => PlainFood(t) && (HasCategory(t, "EggsUnfertilized") || HasCategory(t, "AnimalProductRaw")));
            Add("Food_AnimalFeed", t => PlainFood(t) && (HasCategory(t, "AnimalFeedBag") || HasCategory(t, "Hay")));
            Add("Food_RawSpecial", t => PlainFood(t));

            Add("Resources_Metals", t => t.thingCategories != null && (HasStuffCategory(t, "Metallic") || HasCategory(t, "Metals")));
            Add("Resources_Stone", t => t.thingCategories != null && (HasStuffCategory(t, "Stony") || HasCategory(t, "StoneBlocks") || HasCategory(t, "Chunks")));
            Add("Resources_Textile_Leathers", t => t.thingCategories != null && (HasStuffCategory(t, "Leathery") || HasCategory(t, "Leathers")));
            Add("Resources_Textile_Fabrics", t => t.thingCategories != null && (HasStuffCategory(t, "Fabric") || HasCategory(t, "Textiles")));
            Add("Resources_Textile_Wools", t => t.thingCategories != null && HasCategory(t, "Wools"));
            Add("Resources_Chemical_Raw", t => t.thingCategories != null &&
                (HasCategory(t, "Drugs") || HasCategory(t, "InorganicDrug") || HasCategory(t, "BodyPartsNatural") || HasCategory(t, "ResourcesRaw")));
            Add("Resources_Components", t => t.thingCategories != null && (HasCategory(t, "Manufactured") || HasCategory(t, "Components")));

            return list;
        }

        private static bool HasCategory(ThingDef thingDef, string catDefName) =>
            thingDef.thingCategories != null && thingDef.thingCategories.Any(c => c.defName == catDefName);

        private static bool HasStuffCategory(ThingDef thingDef, string stuffCat) =>
            thingDef.stuffProps?.categories != null && thingDef.stuffProps.categories.Any(c => c.defName == stuffCat);

        private static GameObjectMeta MetaFromThing(ThingDef thingDef) => new GameObjectMeta
        {
            DefName = thingDef.defName,
            Label = thingDef.label ?? thingDef.defName,
            Description = thingDef.description ?? string.Empty,
            MarketValue = thingDef.BaseMarketValue,
            TechLevel = thingDef.techLevel
        };

        private static GameObjectMeta MetaFromDef(Def def) => new GameObjectMeta
        {
            DefName = def.defName,
            Label = def.label ?? def.defName,
            Description = def.description ?? string.Empty,
            MarketValue = 0f
        };

        public static List<GameObjectMeta> GetList(string categoryId) =>
            (IsLoaded && _categories.TryGetValue(categoryId, out var category)) ? category.Items : new List<GameObjectMeta>();

        public static List<string> GetAllowedDefs(string categoryId) =>
            GetList(categoryId).Select(m => m.DefName).ToList();

        private static readonly Random _categoryRandom = new Random();

        public static string DrawRandomCategory()
        {
            var candidates = _categories.Values
                .Where(c => c.Weight > 0f && c.Items.Count > 0)
                .ToList();

            if (candidates.Count == 0)
                candidates = _categories.Values.Where(c => c.Items.Count > 0).ToList();

            if (candidates.Count == 0)
                return "Resources_Components";

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