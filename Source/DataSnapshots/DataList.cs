using RimWorld;
using System.Collections.Generic;
using System.Linq.Expressions;
using Verse;

// I don't know maybe rag is not that wrong of an idea now it is only names but dynamically attaching also descriptions I don't know 
namespace RimTelligence.Source.DataSnapshots
{
    [StaticConstructorOnStartup]
    public class DataList
    {
        public static List<string> WeaponList = new List<string>();
        public static List<string> FoodList = new List<string>();
        public static List<string> ApparelList = new List<string>();
        public static List<string> MedicineList = new List<string>();
        public static List<string> DrugsList = new List<string>();
        public static List<string> AnimalList = new List<string>();
        public static List<string> DiseaseList = new List<string>();
        public static List<string> ResourceList = new List<string>();  // raw materials / stuff
        public static List<string> DamageTypeList = new List<string>();
        public static List<string> GameConditionList = new List<string>();
        public static List<string> AllWeathersList = new List<string>();
        public static List<string> AllSkillsList = new List<string>();

        static DataList() => PopulateItems();

        private static void PopulateItems()
        {

            // define this once outside the loop, at the top of PopulateItems()
            HashSet<string> resourceCategories = new HashSet<string>
            {
                "ResourcesRaw",
                "Manufactured",
                "Textiles",
                "Leathers",
                "Wools",
                "StoneBlocks",
                "Chunks"
            };

            WeaponList.Clear(); FoodList.Clear(); ApparelList.Clear();
            MedicineList.Clear(); DrugsList.Clear(); AnimalList.Clear();
            DiseaseList.Clear();
            ResourceList.Clear();
            DamageTypeList.Clear();
            GameConditionList.Clear();
            AllWeathersList.Clear();
            AllSkillsList.Clear();



            foreach (ThingDef d in DefDatabase<ThingDef>.AllDefs)
            {
                // ── skip abstract / internal defs ──
                if (d.IsBlueprint || d.IsFrame || d.isUnfinishedThing) continue;


                //animals
                if (d.category == ThingCategory.Pawn)
                {
                    if (d.race?.Animal == true)
                        AnimalList.Add(d.defName);
                    continue;
                }

                //everything below is ThingCategory.Item
                if (d.category != ThingCategory.Item) continue;

                
                if (d.IsIngestible && d.IsDrug) DrugsList.Add(d.defName);
                else if (d.IsIngestible) FoodList.Add(d.defName);
                else if (d.IsMedicine) MedicineList.Add(d.defName);
                else if (d.IsWeapon) WeaponList.Add(d.defName);
                else if (d.IsApparel) ApparelList.Add(d.defName);
                else if (d.IsIngestible) FoodList.Add(d.defName);

                else if (d.thingCategories != null &&
                    d.thingCategories.Any(c => resourceCategories.Contains(c.defName)))
                    ResourceList.Add(d.defName);

            }

            //HediffDefs (diseases, conditions, implant hediffs)
            foreach (HediffDef h in DefDatabase<HediffDef>.AllDefs)
            {
                if (h.hediffClass != null &&
                    typeof(Hediff_Injury).IsAssignableFrom(h.hediffClass)) continue;

                DiseaseList.Add(h.defName);
            }

            foreach (DamageDef d in DefDatabase<DamageDef>.AllDefs)
            {
                DamageTypeList.Add(d.defName);
            }

            foreach (GameConditionDef gc in DefDatabase<GameConditionDef>.AllDefs)
            {
                GameConditionList.Add(gc.defName);
            }

            foreach (WeatherDef w in DefDatabase<WeatherDef>.AllDefs)
            {
                AllWeathersList.Add(w.defName);
            }

            foreach (SkillDef s in DefDatabase<SkillDef>.AllDefs)
            {
                AllSkillsList.Add(s.defName);
            }

            WeaponList.Sort(); FoodList.Sort(); ApparelList.Sort();
            MedicineList.Sort(); DrugsList.Sort(); AnimalList.Sort();
            DiseaseList.Sort();
            ResourceList.Sort();
            DamageTypeList.Sort();
            GameConditionList.Sort();
            AllWeathersList.Sort();
            AllSkillsList.Sort();
        }
    }
}
