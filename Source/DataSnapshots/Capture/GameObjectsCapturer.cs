using RimTelligence.Source.DataSnapshots.Model;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using Verse;

namespace RimTelligence.Source.DataSnapshots
{
    [StaticConstructorOnStartup]
    public class GameObjectsCapturer
    {
        // Weapons by tech level
        public static List<GameObjectMeta> WeaponsTribal = new List<GameObjectMeta>();
        public static List<GameObjectMeta> WeaponsIndustrial = new List<GameObjectMeta>();
        public static List<GameObjectMeta> WeaponsSpacer = new List<GameObjectMeta>();
        public static List<GameObjectMeta> WeaponsArchotech = new List<GameObjectMeta>();
        public static List<GameObjectMeta> WeaponsUltra = new List<GameObjectMeta>();
        public static List<GameObjectMeta> WeaponsOther = new List<GameObjectMeta>();

        // Nutrition
        public static List<GameObjectMeta> Food_Meals = new List<GameObjectMeta>();
        public static List<GameObjectMeta> Food_RawPlant = new List<GameObjectMeta>();
        public static List<GameObjectMeta> Food_RawMeat = new List<GameObjectMeta>();
        public static List<GameObjectMeta> Food_RawSpecial = new List<GameObjectMeta>();
        public static List<GameObjectMeta> Food_AnimalFeed = new List<GameObjectMeta>();
        public static List<GameObjectMeta> Food_AnimalProducts = new List<GameObjectMeta>();
        public static List<GameObjectMeta> Animal_Corpses = new List<GameObjectMeta>();

        // Resources
        public static List<GameObjectMeta> Resources_Metals = new List<GameObjectMeta>();
        public static List<GameObjectMeta> Resources_Stone = new List<GameObjectMeta>();
        public static List<GameObjectMeta> Resources_Textile_Leathers = new List<GameObjectMeta>();
        public static List<GameObjectMeta> Resources_Textile_Fabrics = new List<GameObjectMeta>();
        public static List<GameObjectMeta> Resources_Textile_Wools = new List<GameObjectMeta>();
        public static List<GameObjectMeta> Resources_Chemical_Raw = new List<GameObjectMeta>();
        public static List<GameObjectMeta> Resources_Components = new List<GameObjectMeta>();
        public static List<GameObjectMeta> Apparel_Clothing = new List<GameObjectMeta>();

        // Medicine / Drugs
        public static List<GameObjectMeta> MedicineList = new List<GameObjectMeta>();
        public static List<GameObjectMeta> DrugsList = new List<GameObjectMeta>();

        // Animals
        public static List<GameObjectMeta> AnimalList = new List<GameObjectMeta>();

        // Misc
        public static List<GameObjectMeta> DiseaseList = new List<GameObjectMeta>();
        public static List<GameObjectMeta> DamageTypeList = new List<GameObjectMeta>();
        public static List<GameObjectMeta> GameConditionList = new List<GameObjectMeta>();
        public static List<GameObjectMeta> AllWeathersList = new List<GameObjectMeta>();
        public static List<GameObjectMeta> AllSkillsList = new List<GameObjectMeta>();

        static GameObjectsCapturer()
        {
            PopulateItems();
            //DataExporter.ExportAll(); uncomment for file saved on desktop 
        }
        //  Helpers 

        private static GameObjectMeta MetaFromThing(ThingDef d) => new GameObjectMeta
        {
            DefName = d.defName,
            Label = d.label ?? d.defName,
            Description = d.description ?? string.Empty,
            MarketValue = d.BaseMarketValue,
            TechLevel = d.techLevel
        };

        private static GameObjectMeta MetaFromDef(Def d) => new GameObjectMeta
        {
            DefName = d.defName,
            Label = d.label ?? d.defName,
            Description = d.description ?? string.Empty,
            MarketValue = 0f
        };

        private static void SortByDefName(List<GameObjectMeta> list) =>
            list.Sort((a, b) => string.Compare(a.DefName, b.DefName, StringComparison.Ordinal));

        private static bool HasCategory(ThingDef d, string catDefName) =>
            d.thingCategories != null &&
            d.thingCategories.Any(c => c.defName == catDefName);

        private static bool HasStuffCategory(ThingDef d, string stuffCat) =>
            d.stuffProps?.categories != null &&
            d.stuffProps.categories.Any(c => c.defName == stuffCat);

        //  Population 

        private static void PopulateItems()
        {
            var allLists = new[]
            {
                WeaponsTribal, WeaponsIndustrial, WeaponsSpacer,
                WeaponsArchotech, WeaponsUltra, WeaponsOther,
                Food_Meals, Food_RawPlant, Food_RawMeat, Food_RawSpecial,
                Food_AnimalFeed, Food_AnimalProducts, Animal_Corpses,
                Resources_Metals, Resources_Stone,
                Resources_Textile_Leathers, Resources_Textile_Fabrics, Resources_Textile_Wools,
                Resources_Chemical_Raw, Resources_Components,
                Apparel_Clothing,
                MedicineList, DrugsList, AnimalList,
                DiseaseList, DamageTypeList, GameConditionList,
                AllWeathersList, AllSkillsList
            };
            foreach (var list in allLists) list.Clear();

            foreach (ThingDef d in DefDatabase<ThingDef>.AllDefs)
            {
                if (d.IsBlueprint || d.IsFrame || d.isUnfinishedThing) continue;

                //  Corpses 
                if (d.IsCorpse)
                {
                    Animal_Corpses.Add(MetaFromThing(d));
                    continue;
                }

                //  Animals 
                if (d.category == ThingCategory.Pawn)
                {
                    if (d.race?.Animal == true)
                        AnimalList.Add(MetaFromThing(d));
                    continue;
                }

                if (d.category != ThingCategory.Item) continue;

                //  Weapons 
                if (d.IsWeapon)
                {
                    switch (d.techLevel)
                    {
                        case TechLevel.Neolithic:
                        case TechLevel.Medieval: WeaponsTribal.Add(MetaFromThing(d)); break;
                        case TechLevel.Industrial: WeaponsIndustrial.Add(MetaFromThing(d)); break;
                        case TechLevel.Spacer: WeaponsSpacer.Add(MetaFromThing(d)); break;
                        case TechLevel.Archotech: WeaponsArchotech.Add(MetaFromThing(d)); break;
                        case TechLevel.Ultra: WeaponsUltra.Add(MetaFromThing(d)); break;
                        default: WeaponsOther.Add(MetaFromThing(d)); break;
                    }
                    continue;
                }

                //  Drugs 
                if (d.IsIngestible && d.IsDrug)
                {
                    DrugsList.Add(MetaFromThing(d));
                    continue;
                }

                //  Medicine 
                if (d.IsMedicine)
                {
                    MedicineList.Add(MetaFromThing(d));
                    continue;
                }

                //  Apparel 
                if (d.IsApparel)
                {
                    Apparel_Clothing.Add(MetaFromThing(d));
                    continue;
                }

                //  Food 
                if (d.IsIngestible)
                {
                    if (HasCategory(d, "FoodMeals"))
                        Food_Meals.Add(MetaFromThing(d));
                    else if (HasCategory(d, "PlantFoodRaw") || HasCategory(d, "PlantMatter"))
                        Food_RawPlant.Add(MetaFromThing(d));
                    else if (HasCategory(d, "MeatRaw"))
                        Food_RawMeat.Add(MetaFromThing(d));
                    else if (HasCategory(d, "EggsUnfertilized") || HasCategory(d, "AnimalProductRaw"))
                        Food_AnimalProducts.Add(MetaFromThing(d));
                    else if (HasCategory(d, "AnimalFeedBag") || HasCategory(d, "Hay"))
                        Food_AnimalFeed.Add(MetaFromThing(d));
                    else
                        Food_RawSpecial.Add(MetaFromThing(d)); // kibble, pemmican, exotic foods
                    continue;
                }

                //  Resources 
                if (d.thingCategories == null) continue;

                if (HasStuffCategory(d, "Metallic") || HasCategory(d, "Metals"))
                    Resources_Metals.Add(MetaFromThing(d));
                else if (HasStuffCategory(d, "Stony") || HasCategory(d, "StoneBlocks") || HasCategory(d, "Chunks"))
                    Resources_Stone.Add(MetaFromThing(d));
                else if (HasStuffCategory(d, "Leathery") || HasCategory(d, "Leathers"))
                    Resources_Textile_Leathers.Add(MetaFromThing(d));
                else if (HasStuffCategory(d, "Fabric") || HasCategory(d, "Textiles"))
                    Resources_Textile_Fabrics.Add(MetaFromThing(d));
                else if (HasCategory(d, "Wools"))
                    Resources_Textile_Wools.Add(MetaFromThing(d));
                else if (HasCategory(d, "Drugs") || HasCategory(d, "InorganicDrug") ||
                         HasCategory(d, "BodyPartsNatural") || HasCategory(d, "ResourcesRaw"))
                    Resources_Chemical_Raw.Add(MetaFromThing(d));
                else if (HasCategory(d, "Manufactured") || HasCategory(d, "Components"))
                    Resources_Components.Add(MetaFromThing(d));
            }

            //  HediffDefs 
            foreach (HediffDef h in DefDatabase<HediffDef>.AllDefs)
            {
                if (h.hediffClass != null &&
                    typeof(Hediff_Injury).IsAssignableFrom(h.hediffClass)) continue;
                DiseaseList.Add(MetaFromDef(h));
            }

            foreach (DamageDef d in DefDatabase<DamageDef>.AllDefs) DamageTypeList.Add(MetaFromDef(d));
            foreach (GameConditionDef gc in DefDatabase<GameConditionDef>.AllDefs) GameConditionList.Add(MetaFromDef(gc));
            foreach (WeatherDef w in DefDatabase<WeatherDef>.AllDefs) AllWeathersList.Add(MetaFromDef(w));
            foreach (SkillDef s in DefDatabase<SkillDef>.AllDefs) AllSkillsList.Add(MetaFromDef(s));

            foreach (var list in allLists) SortByDefName(list);
        }
    }
}