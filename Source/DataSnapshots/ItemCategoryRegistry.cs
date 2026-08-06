using Newtonsoft.Json;
using RimTelligence.CustomEvents;
using RimWorld;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Verse;
using RimTelligence.CustomEvents.Handlers;

namespace RimTelligence.DataSnapshots
{
    public class ItemCategory
    {
        public string Id;
        public string Label;
        public int SortOrder;
        public List<GameObjectMeta> Items = new List<GameObjectMeta>();
        public Dictionary<string, ItemCategoryVariant> Variants = new Dictionary<string, ItemCategoryVariant>();
    }

    public class ItemCategoryVariant
    {
        public string Description;
        public float Weight;
        public EventAlignment Alignment;
    }

    public class ItemCategoryConfig
    {
        public List<ItemCategoryConfigEntry> Categories;
    }

    public class ItemCategoryConfigEntry
    {
        public string Id;
        public string Label;
        public int SortOrder;
        public Dictionary<string, ItemCategoryVariantConfig> Variants;
    }

    public class ItemCategoryVariantConfig
    {
        public string Description;
        public float Weight;
        public string Alignment;
    }

    [StaticConstructorOnStartup]
    public static class ItemCategoryRegistry
    {
        private static string ConfigPath => _configPath.Value;
        private static readonly Lazy<string> _configPath = new Lazy<string>(() =>
        {
            string assembliesDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            string modRoot = Path.GetFullPath(Path.Combine(assembliesDir, ".."));
            return Path.Combine(modRoot, "Data", "item-categories.json");
        });

        private static readonly Dictionary<string, string> VariantPrefixMap =
            new Dictionary<string, string>
            {
                { "pod", "ResourcePodCrash" },
                { "found", "PawnFoundItem" },
                { "lose", "LoseItems" }
            };

        public static bool IsLoaded { get; private set; }

        public static IReadOnlyDictionary<string, ItemCategory> AllCategories => _categories;

        private static Dictionary<string, ItemCategory> _categories =
            new Dictionary<string, ItemCategory>();

        static ItemCategoryRegistry()
        {
            LoadConfig();
        }

        public static void LoadConfig()
        {
            try
            {
                if (!File.Exists(ConfigPath))
                {
                    ModLogger.Message("Item categories config not found at " + ConfigPath +
                        ". Using empty registry.");
                    IsLoaded = false;
                    return;
                }

                string json = File.ReadAllText(ConfigPath);
                var config = JsonConvert.DeserializeObject<ItemCategoryConfig>(json);

                if (config?.Categories == null || config.Categories.Count == 0)
                {
                    ModLogger.Message("Item categories config is empty or invalid. Using empty registry.");
                    IsLoaded = false;
                    return;
                }

                var categories = new Dictionary<string, ItemCategory>();
                foreach (var entry in config.Categories)
                {
                    if (string.IsNullOrEmpty(entry.Id))
                    {
                        ModLogger.Message("Skipping category with missing id in config.");
                        continue;
                    }

                    if (categories.ContainsKey(entry.Id))
                    {
                        ModLogger.Message("Duplicate category id '" + entry.Id + "' in config. Skipping duplicate.");
                        continue;
                    }

                    var category = new ItemCategory
                    {
                        Id = entry.Id,
                        Label = entry.Label ?? entry.Id,
                        SortOrder = entry.SortOrder
                    };

                    if (entry.Variants != null)
                    {
                        foreach (var kvp in entry.Variants)
                        {
                            if (kvp.Value == null) continue;

                            if (!Enum.TryParse<EventAlignment>(kvp.Value.Alignment, true, out var alignment))
                            {
                                ModLogger.Message("Invalid alignment '" + kvp.Value.Alignment +
                                    "' for category '" + entry.Id + "' variant '" + kvp.Key + "'. Skipping.");
                                continue;
                            }

                            category.Variants[kvp.Key] = new ItemCategoryVariant
                            {
                                Description = kvp.Value.Description ?? "",
                                Weight = kvp.Value.Weight,
                                Alignment = alignment
                            };
                        }
                    }

                    categories[entry.Id] = category;
                }

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

        public static void PopulateAll()
        {
            foreach (var items in _categories.Values)
                items.Items.Clear();

            var itemPredicates = BuildItemPredicates();

            foreach (ThingDef thingDef in DefDatabase<ThingDef>.AllDefs)
            {
                if (thingDef.IsBlueprint || thingDef.IsFrame || thingDef.isUnfinishedThing) continue;

                if (thingDef.IsCorpse)
                {
                    TryAdd("Animal_Corpses", thingDef);
                    continue;
                }

                if (thingDef.category == ThingCategory.Pawn)
                {
                    if (thingDef.race?.Animal == true)
                        TryAdd("AnimalList", thingDef);
                    continue;
                }

                if (thingDef.category != ThingCategory.Item) continue;

                foreach (var pred in itemPredicates)
                {
                    if (pred.predicate(thingDef))
                    {
                        TryAdd(pred.id, thingDef);
                        break;
                    }
                }
            }

            TryAddDefs("DiseaseList", DefDatabase<HediffDef>.AllDefs,
                hediffDef => !(hediffDef.hediffClass != null && typeof(Hediff_Injury).IsAssignableFrom(hediffDef.hediffClass)));
            TryAddDefs("DamageTypeList", DefDatabase<DamageDef>.AllDefs, null);
            TryAddDefs("GameConditionList", DefDatabase<GameConditionDef>.AllDefs, null);
            TryAddDefs("AllWeathersList", DefDatabase<WeatherDef>.AllDefs, null);
            TryAddDefs("AllSkillsList", DefDatabase<SkillDef>.AllDefs, null);

            foreach (var list in _categories.Values)
            {
                if (list.Items.Count > 0)
                    list.Items.Sort((leftItem, rightItem) => string.Compare(leftItem.DefName, rightItem.DefName, StringComparison.Ordinal));
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

            void Add(string id, Func<ThingDef, bool> predicate) { list.Add((id, predicate)); }

            Add("WeaponsTribal", thingDef => thingDef.IsWeapon && (thingDef.techLevel == TechLevel.Neolithic || thingDef.techLevel == TechLevel.Medieval));
            Add("WeaponsIndustrial", thingDef => thingDef.IsWeapon && thingDef.techLevel == TechLevel.Industrial);
            Add("WeaponsSpacer", thingDef => thingDef.IsWeapon && thingDef.techLevel == TechLevel.Spacer);
            Add("WeaponsArchotech", thingDef => thingDef.IsWeapon && thingDef.techLevel == TechLevel.Archotech);
            Add("WeaponsUltra", thingDef => thingDef.IsWeapon && thingDef.techLevel == TechLevel.Ultra);
            Add("WeaponsOther", thingDef => thingDef.IsWeapon && thingDef.category == ThingCategory.Item);

            Add("DrugsList", thingDef => thingDef.IsIngestible && thingDef.IsDrug);
            Add("MedicineList", thingDef => thingDef.IsMedicine);
            Add("Apparel_Clothing", thingDef => thingDef.IsApparel);

            Add("Food_Meals", thingDef => thingDef.IsIngestible && !thingDef.IsDrug && !thingDef.IsMedicine && HasCategory(thingDef, "FoodMeals"));
            Add("Food_RawPlant", thingDef => thingDef.IsIngestible && !thingDef.IsDrug && !thingDef.IsMedicine &&
                (HasCategory(thingDef, "PlantFoodRaw") || HasCategory(thingDef, "PlantMatter")));
            Add("Food_RawMeat", thingDef => thingDef.IsIngestible && !thingDef.IsDrug && !thingDef.IsMedicine && HasCategory(thingDef, "MeatRaw"));
            Add("Food_AnimalProducts", thingDef => thingDef.IsIngestible && !thingDef.IsDrug && !thingDef.IsMedicine &&
                (HasCategory(thingDef, "EggsUnfertilized") || HasCategory(thingDef, "AnimalProductRaw")));
            Add("Food_AnimalFeed", thingDef => thingDef.IsIngestible && !thingDef.IsDrug && !thingDef.IsMedicine &&
                (HasCategory(thingDef, "AnimalFeedBag") || HasCategory(thingDef, "Hay")));
            Add("Food_RawSpecial", thingDef => thingDef.IsIngestible && !thingDef.IsDrug && !thingDef.IsMedicine);

            Add("Resources_Metals", thingDef => thingDef.thingCategories != null &&
                (HasStuffCategory(thingDef, "Metallic") || HasCategory(thingDef, "Metals")));
            Add("Resources_Stone", thingDef => thingDef.thingCategories != null &&
                (HasStuffCategory(thingDef, "Stony") || HasCategory(thingDef, "StoneBlocks") || HasCategory(thingDef, "Chunks")));
            Add("Resources_Textile_Leathers", thingDef => thingDef.thingCategories != null &&
                (HasStuffCategory(thingDef, "Leathery") || HasCategory(thingDef, "Leathers")));
            Add("Resources_Textile_Fabrics", thingDef => thingDef.thingCategories != null &&
                (HasStuffCategory(thingDef, "Fabric") || HasCategory(thingDef, "Textiles")));
            Add("Resources_Textile_Wools", thingDef => thingDef.thingCategories != null && HasCategory(thingDef, "Wools"));
            Add("Resources_Chemical_Raw", thingDef => thingDef.thingCategories != null &&
                (HasCategory(thingDef, "Drugs") || HasCategory(thingDef, "InorganicDrug") ||
                 HasCategory(thingDef, "BodyPartsNatural") || HasCategory(thingDef, "ResourcesRaw")));
            Add("Resources_Components", thingDef => thingDef.thingCategories != null &&
                (HasCategory(thingDef, "Manufactured") || HasCategory(thingDef, "Components")));

            return list;
        }

        private static bool HasCategory(ThingDef thingDef, string catDefName)
        {
            return thingDef.thingCategories != null &&
                   thingDef.thingCategories.Any(categoryDef => categoryDef.defName == catDefName);
        }

        private static bool HasStuffCategory(ThingDef thingDef, string stuffCat)
        {
            return thingDef.stuffProps?.categories != null &&
                   thingDef.stuffProps.categories.Any(categoryDef => categoryDef.defName == stuffCat);
        }

        private static GameObjectMeta MetaFromThing(ThingDef thingDef)
        {
            return new GameObjectMeta
            {
                DefName = thingDef.defName,
                Label = thingDef.label ?? thingDef.defName,
                Description = thingDef.description ?? string.Empty,
                MarketValue = thingDef.BaseMarketValue,
                TechLevel = thingDef.techLevel
            };
        }

        private static GameObjectMeta MetaFromDef(Def def)
        {
            return new GameObjectMeta
            {
                DefName = def.defName,
                Label = def.label ?? def.defName,
                Description = def.description ?? string.Empty,
                MarketValue = 0f
            };
        }

        public static List<GameObjectMeta> GetList(string categoryId)
        {
            if (!IsLoaded || !_categories.TryGetValue(categoryId, out var category))
                return new List<GameObjectMeta>();
            return category.Items;
        }

        public static List<string> GetAllowedDefs(string categoryId)
        {
            var list = GetList(categoryId);
            if (list == null || list.Count == 0)
                return new List<string>();
            return list.Select(meta => meta.DefName).ToList();
        }

        public static ItemCategoryVariant GetVariant(string categoryId, string variantPrefix)
        {
            if (!IsLoaded || !_categories.TryGetValue(categoryId, out var category))
                return null;
            category.Variants.TryGetValue(variantPrefix, out var variant);
            return variant;
        }

        public static string GetVariantPrefix(string shortKey)
        {
            VariantPrefixMap.TryGetValue(shortKey, out var prefix);
            return prefix;
        }

        public static IEnumerable<string> AllVariantKeys => VariantPrefixMap.Keys;
    }
}
