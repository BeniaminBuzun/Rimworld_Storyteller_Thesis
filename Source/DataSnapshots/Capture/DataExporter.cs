using RimTelligence.Source.DataSnapshots.Model;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using Verse;

namespace RimTelligence.Source.DataSnapshots
{
    public static class DataExporter
    {
        private static readonly string OutputPath = @"C:\Users\Karol\Desktop\resources.json";

        public static void ExportAll()
        {
            try
            {
                var snapshot = new DataSnapshot
                {
                    // Weapons
                    WeaponsTribal = GameObjectsCapturer.WeaponsTribal,
                    WeaponsIndustrial = GameObjectsCapturer.WeaponsIndustrial,
                    WeaponsSpacer = GameObjectsCapturer.WeaponsSpacer,
                    WeaponsArchotech = GameObjectsCapturer.WeaponsArchotech,
                    WeaponsUltra = GameObjectsCapturer.WeaponsUltra,
                    WeaponsOther = GameObjectsCapturer.WeaponsOther,

                    // Nutrition
                    Food_Meals = GameObjectsCapturer.Food_Meals,
                    Food_RawPlant = GameObjectsCapturer.Food_RawPlant,
                    Food_RawMeat = GameObjectsCapturer.Food_RawMeat,
                    Food_RawSpecial = GameObjectsCapturer.Food_RawSpecial,
                    Food_AnimalFeed = GameObjectsCapturer.Food_AnimalFeed,
                    Food_AnimalProducts = GameObjectsCapturer.Food_AnimalProducts,
                    Animal_Corpses = GameObjectsCapturer.Animal_Corpses,

                    // Resources
                    Resources_Metals = GameObjectsCapturer.Resources_Metals,
                    Resources_Stone = GameObjectsCapturer.Resources_Stone,
                    Resources_Textile_Leathers = GameObjectsCapturer.Resources_Textile_Leathers,
                    Resources_Textile_Fabrics = GameObjectsCapturer.Resources_Textile_Fabrics,
                    Resources_Textile_Wools = GameObjectsCapturer.Resources_Textile_Wools,
                    Resources_Chemical_Raw = GameObjectsCapturer.Resources_Chemical_Raw,
                    Resources_Components = GameObjectsCapturer.Resources_Components,
                    Apparel_Clothing = GameObjectsCapturer.Apparel_Clothing,

                    // Medicine / Drugs
                    MedicineList = GameObjectsCapturer.MedicineList,
                    DrugsList = GameObjectsCapturer.DrugsList,

                    // Animals
                    AnimalList = GameObjectsCapturer.AnimalList,

                    // Misc
                    DiseaseList = GameObjectsCapturer.DiseaseList,
                    DamageTypeList = GameObjectsCapturer.DamageTypeList,
                    GameConditionList = GameObjectsCapturer.GameConditionList,
                    AllWeathersList = GameObjectsCapturer.AllWeathersList,
                    AllSkillsList = GameObjectsCapturer.AllSkillsList,
                };

                string json = JsonConvert.SerializeObject(snapshot, Formatting.Indented);
                File.WriteAllText(OutputPath, json);

                Log.Message($"[RimTelligence] Data exported to: {OutputPath}");
            }
            catch (Exception e)
            {
                Log.Error($"[RimTelligence] Failed to export data: {e}");
            }
        }

        public class DataSnapshot
        {
            // Weapons
            public List<GameObjectMeta> WeaponsTribal { get; set; }
            public List<GameObjectMeta> WeaponsIndustrial { get; set; }
            public List<GameObjectMeta> WeaponsSpacer { get; set; }
            public List<GameObjectMeta> WeaponsArchotech { get; set; }
            public List<GameObjectMeta> WeaponsUltra { get; set; }
            public List<GameObjectMeta> WeaponsOther { get; set; }

            // Nutrition
            public List<GameObjectMeta> Food_Meals { get; set; }
            public List<GameObjectMeta> Food_RawPlant { get; set; }
            public List<GameObjectMeta> Food_RawMeat { get; set; }
            public List<GameObjectMeta> Food_RawSpecial { get; set; }
            public List<GameObjectMeta> Food_AnimalFeed { get; set; }
            public List<GameObjectMeta> Food_AnimalProducts { get; set; }
            public List<GameObjectMeta> Animal_Corpses { get; set; }

            // Resources
            public List<GameObjectMeta> Resources_Metals { get; set; }
            public List<GameObjectMeta> Resources_Stone { get; set; }
            public List<GameObjectMeta> Resources_Textile_Leathers { get; set; }
            public List<GameObjectMeta> Resources_Textile_Fabrics { get; set; }
            public List<GameObjectMeta> Resources_Textile_Wools { get; set; }
            public List<GameObjectMeta> Resources_Chemical_Raw { get; set; }
            public List<GameObjectMeta> Resources_Components { get; set; }
            public List<GameObjectMeta> Apparel_Clothing { get; set; }

            // Medicine / Drugs
            public List<GameObjectMeta> MedicineList { get; set; }
            public List<GameObjectMeta> DrugsList { get; set; }

            // Animals
            public List<GameObjectMeta> AnimalList { get; set; }

            // Misc
            public List<GameObjectMeta> DiseaseList { get; set; }
            public List<GameObjectMeta> DamageTypeList { get; set; }
            public List<GameObjectMeta> GameConditionList { get; set; }
            public List<GameObjectMeta> AllWeathersList { get; set; }
            public List<GameObjectMeta> AllSkillsList { get; set; }
        }
    }
}