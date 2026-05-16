using RimTelligence.Source.DataSnapshots;
using System.Collections.Generic;
using System.Linq;

namespace RimTelligence.Source.CustomEvents.Handlers.Implementations.ItemsRelated
{
    public static class EventItemMapping
    {
        public static List<string> GetAllowedDefs(string effectType)
        {
            switch (effectType)
            {
                //
                //
                //
                //
                //
                //  ResourcePodCrash 
                case "ResourcePodCrash_WeaponTribal": return Names(GameObjectsCapturer.WeaponsTribal);
                case "ResourcePodCrash_WeaponIndustrial": return Names(GameObjectsCapturer.WeaponsIndustrial);
                case "ResourcePodCrash_WeaponSpacer": return Names(GameObjectsCapturer.WeaponsSpacer);
                case "ResourcePodCrash_WeaponArchotech": return Names(GameObjectsCapturer.WeaponsArchotech);
                case "ResourcePodCrash_WeaponUltra": return Names(GameObjectsCapturer.WeaponsUltra);
                case "ResourcePodCrash_WeaponOther": return Names(GameObjectsCapturer.WeaponsOther);
                case "ResourcePodCrash_FoodMeals": return Names(GameObjectsCapturer.Food_Meals);
                case "ResourcePodCrash_FoodRawPlant": return Names(GameObjectsCapturer.Food_RawPlant);
                case "ResourcePodCrash_FoodRawMeat": return Names(GameObjectsCapturer.Food_RawMeat);
                case "ResourcePodCrash_FoodRawSpecial": return Names(GameObjectsCapturer.Food_RawSpecial);
                case "ResourcePodCrash_FoodAnimalProducts": return Names(GameObjectsCapturer.Food_AnimalProducts);
                case "ResourcePodCrash_Metals": return Names(GameObjectsCapturer.Resources_Metals);
                case "ResourcePodCrash_Stone": return Names(GameObjectsCapturer.Resources_Stone);
                case "ResourcePodCrash_TextileLeathers": return Names(GameObjectsCapturer.Resources_Textile_Leathers);
                case "ResourcePodCrash_TextileFabrics": return Names(GameObjectsCapturer.Resources_Textile_Fabrics);
                case "ResourcePodCrash_TextileWools": return Names(GameObjectsCapturer.Resources_Textile_Wools);
                case "ResourcePodCrash_Chemicals": return Names(GameObjectsCapturer.Resources_Chemical_Raw);
                case "ResourcePodCrash_Components": return Names(GameObjectsCapturer.Resources_Components);
                case "ResourcePodCrash_Apparel": return Names(GameObjectsCapturer.Apparel_Clothing);
                case "ResourcePodCrash_Medicine": return Names(GameObjectsCapturer.MedicineList);
                case "ResourcePodCrash_Drugs": return Names(GameObjectsCapturer.DrugsList);

                //  PawnFoundItem 
                case "PawnFoundItem_WeaponTribal": return Names(GameObjectsCapturer.WeaponsTribal);
                case "PawnFoundItem_WeaponIndustrial": return Names(GameObjectsCapturer.WeaponsIndustrial);
                case "PawnFoundItem_WeaponSpacer": return Names(GameObjectsCapturer.WeaponsSpacer);
                case "PawnFoundItem_WeaponArchotech": return Names(GameObjectsCapturer.WeaponsArchotech);
                case "PawnFoundItem_WeaponUltra": return Names(GameObjectsCapturer.WeaponsUltra);
                case "PawnFoundItem_FoodMeals": return Names(GameObjectsCapturer.Food_Meals);
                case "PawnFoundItem_FoodRawPlant": return Names(GameObjectsCapturer.Food_RawPlant);
                case "PawnFoundItem_FoodRawMeat": return Names(GameObjectsCapturer.Food_RawMeat);
                case "PawnFoundItem_FoodRawSpecial": return Names(GameObjectsCapturer.Food_RawSpecial);
                case "PawnFoundItem_FoodAnimalProducts": return Names(GameObjectsCapturer.Food_AnimalProducts);
                case "PawnFoundItem_Metals": return Names(GameObjectsCapturer.Resources_Metals);
                case "PawnFoundItem_Stone": return Names(GameObjectsCapturer.Resources_Stone);
                case "PawnFoundItem_TextileLeathers": return Names(GameObjectsCapturer.Resources_Textile_Leathers);
                case "PawnFoundItem_TextileFabrics": return Names(GameObjectsCapturer.Resources_Textile_Fabrics);
                case "PawnFoundItem_TextileWools": return Names(GameObjectsCapturer.Resources_Textile_Wools);
                case "PawnFoundItem_Chemicals": return Names(GameObjectsCapturer.Resources_Chemical_Raw);
                case "PawnFoundItem_Components": return Names(GameObjectsCapturer.Resources_Components);
                case "PawnFoundItem_Apparel": return Names(GameObjectsCapturer.Apparel_Clothing);
                case "PawnFoundItem_Medicine": return Names(GameObjectsCapturer.MedicineList);
                case "PawnFoundItem_Drugs": return Names(GameObjectsCapturer.DrugsList);

                //  LoseItems 
                case "LoseItems_WeaponTribal": return Names(GameObjectsCapturer.WeaponsTribal);
                case "LoseItems_WeaponIndustrial": return Names(GameObjectsCapturer.WeaponsIndustrial);
                case "LoseItems_WeaponSpacer": return Names(GameObjectsCapturer.WeaponsSpacer);
                case "LoseItems_FoodMeals": return Names(GameObjectsCapturer.Food_Meals);
                case "LoseItems_FoodRawPlant": return Names(GameObjectsCapturer.Food_RawPlant);
                case "LoseItems_FoodRawMeat": return Names(GameObjectsCapturer.Food_RawMeat);
                case "LoseItems_Metals": return Names(GameObjectsCapturer.Resources_Metals);
                case "LoseItems_Stone": return Names(GameObjectsCapturer.Resources_Stone);
                case "LoseItems_TextileLeathers": return Names(GameObjectsCapturer.Resources_Textile_Leathers);
                case "LoseItems_TextileFabrics": return Names(GameObjectsCapturer.Resources_Textile_Fabrics);
                case "LoseItems_TextileWools": return Names(GameObjectsCapturer.Resources_Textile_Wools);
                case "LoseItems_Chemicals": return Names(GameObjectsCapturer.Resources_Chemical_Raw);
                case "LoseItems_Components": return Names(GameObjectsCapturer.Resources_Components);
                case "LoseItems_Apparel": return Names(GameObjectsCapturer.Apparel_Clothing);
                case "LoseItems_Medicine": return Names(GameObjectsCapturer.MedicineList);
                case "LoseItems_Drugs": return Names(GameObjectsCapturer.DrugsList);

                default:
                    return new List<string>();
            }
        }

        private static List<string> Names(List<RimTelligence.Source.DataSnapshots.Model.GameObjectMeta> source)
        {
            if (source == null) return new List<string>();
            return source.Select(d => d.DefName).ToList();
        }
    }
}