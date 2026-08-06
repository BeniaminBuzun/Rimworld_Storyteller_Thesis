using RimTelligence.CustomEvents;
using RimTelligence.DataSnapshots;
using RimTelligence.Memory;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using Verse;

namespace RimTelligence.DataSnapshots
{
    [StaticConstructorOnStartup]
    public class GameObjectsCapturer
    {
        // Pass-through properties for backward compatibility
        public static List<GameObjectMeta> WeaponsTribal => ItemCategoryRegistry.GetList("WeaponsTribal");
        public static List<GameObjectMeta> WeaponsIndustrial => ItemCategoryRegistry.GetList("WeaponsIndustrial");
        public static List<GameObjectMeta> WeaponsSpacer => ItemCategoryRegistry.GetList("WeaponsSpacer");
        public static List<GameObjectMeta> WeaponsArchotech => ItemCategoryRegistry.GetList("WeaponsArchotech");
        public static List<GameObjectMeta> WeaponsUltra => ItemCategoryRegistry.GetList("WeaponsUltra");
        public static List<GameObjectMeta> WeaponsOther => ItemCategoryRegistry.GetList("WeaponsOther");

        public static List<GameObjectMeta> Food_Meals => ItemCategoryRegistry.GetList("Food_Meals");
        public static List<GameObjectMeta> Food_RawPlant => ItemCategoryRegistry.GetList("Food_RawPlant");
        public static List<GameObjectMeta> Food_RawMeat => ItemCategoryRegistry.GetList("Food_RawMeat");
        public static List<GameObjectMeta> Food_RawSpecial => ItemCategoryRegistry.GetList("Food_RawSpecial");
        public static List<GameObjectMeta> Food_AnimalFeed => ItemCategoryRegistry.GetList("Food_AnimalFeed");
        public static List<GameObjectMeta> Food_AnimalProducts => ItemCategoryRegistry.GetList("Food_AnimalProducts");
        public static List<GameObjectMeta> Animal_Corpses => ItemCategoryRegistry.GetList("Animal_Corpses");

        public static List<GameObjectMeta> Resources_Metals => ItemCategoryRegistry.GetList("Resources_Metals");
        public static List<GameObjectMeta> Resources_Stone => ItemCategoryRegistry.GetList("Resources_Stone");
        public static List<GameObjectMeta> Resources_Textile_Leathers => ItemCategoryRegistry.GetList("Resources_Textile_Leathers");
        public static List<GameObjectMeta> Resources_Textile_Fabrics => ItemCategoryRegistry.GetList("Resources_Textile_Fabrics");
        public static List<GameObjectMeta> Resources_Textile_Wools => ItemCategoryRegistry.GetList("Resources_Textile_Wools");
        public static List<GameObjectMeta> Resources_Chemical_Raw => ItemCategoryRegistry.GetList("Resources_Chemical_Raw");
        public static List<GameObjectMeta> Resources_Components => ItemCategoryRegistry.GetList("Resources_Components");
        public static List<GameObjectMeta> Apparel_Clothing => ItemCategoryRegistry.GetList("Apparel_Clothing");

        public static List<GameObjectMeta> MedicineList => ItemCategoryRegistry.GetList("MedicineList");
        public static List<GameObjectMeta> DrugsList => ItemCategoryRegistry.GetList("DrugsList");

        public static List<GameObjectMeta> AnimalList => ItemCategoryRegistry.GetList("AnimalList");

        public static List<GameObjectMeta> DiseaseList => ItemCategoryRegistry.GetList("DiseaseList");
        public static List<GameObjectMeta> DamageTypeList => ItemCategoryRegistry.GetList("DamageTypeList");
        public static List<GameObjectMeta> GameConditionList => ItemCategoryRegistry.GetList("GameConditionList");
        public static List<GameObjectMeta> AllWeathersList => ItemCategoryRegistry.GetList("AllWeathersList");
        public static List<GameObjectMeta> AllSkillsList => ItemCategoryRegistry.GetList("AllSkillsList");

        public static readonly IEmbeddingProvider EmbeddingProvider = new OnnxEmbeddingProvider();

        static GameObjectsCapturer()
        {
            PopulateItems();
        }

        private static void PopulateItems()
        {
            ItemCategoryRegistry.PopulateAll();
        }

        public static void PopulateVectorStore()
        {
            var store = new StaticRagStore(RagBootstrap.DbPath);
            store.ClearAll();

            foreach (var kvp in ItemCategoryRegistry.AllCategories)
            {
                string category = kvp.Key;
                var items = kvp.Value.Items;

                foreach (var item in items)
                {
                    string text = string.IsNullOrEmpty(item.Description)
                        ? item.Label
                        : item.Label + ": " + item.Description;

                    float[] embedding = EmbeddingProvider.Embed(text);
                    store.AddChunk(category, item.DefName, text, embedding);
                }
            }

            ModLogger.Message("Static vector store populated from config-driven categories.");
        }
    }
}
