using Newtonsoft.Json;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using Verse;

namespace RimTelligence.DataSnapshots
{
    /// <summary>
    /// Reads the live game defs. Its ONLY job is to feed the database sync;
    /// nothing else in the mod reads defs for these lists.
    /// Must run on the main thread (DefDatabase is not thread-safe).
    /// </summary>
    public static class GameDefScanner
    {
        public static bool DefsAvailable()
        {
            try { return DefDatabase<ThingDef>.AllDefs != null && DefDatabase<ThingDef>.AllDefs.Any(); }
            catch { return false; }
        }

        /// <summary>Returns categoryId -> defs, restricted to the known category ids.</summary>
        public static Dictionary<string, List<GameObjectMeta>> Scan(ICollection<string> knownCategoryIds)
        {
            var result = new Dictionary<string, List<GameObjectMeta>>(StringComparer.Ordinal);
            foreach (var id in knownCategoryIds)
                result[id] = new List<GameObjectMeta>();

            if (!DefsAvailable()) return result;

            var itemPredicates = BuildItemPredicates();

            void TryAddThing(string categoryId, ThingDef def)
            {
                if (result.TryGetValue(categoryId, out var list))
                    list.Add(MetaFromThing(def));
            }

            foreach (ThingDef thingDef in DefDatabase<ThingDef>.AllDefs)
            {
                if (thingDef.IsBlueprint || thingDef.IsFrame || thingDef.isUnfinishedThing) continue;

                if (thingDef.IsCorpse) { TryAddThing("Animal_Corpses", thingDef); continue; }

                if (thingDef.category == ThingCategory.Pawn)
                {
                    if (thingDef.race?.Animal == true) TryAddThing("All_Animals", thingDef);
                    continue;
                }

                if (thingDef.category != ThingCategory.Item) continue;

                foreach (var pred in itemPredicates)
                {
                    if (pred.predicate(thingDef)) { TryAddThing(pred.id, thingDef); break; }
                }
            }

            TryAddDefs(result, "Diseases", DefDatabase<HediffDef>.AllDefs,
                h => !(h.hediffClass != null && typeof(Hediff_Injury).IsAssignableFrom(h.hediffClass)));
            TryAddDefs(result, "Damage_Types", DefDatabase<DamageDef>.AllDefs, null);
            TryAddDefs(result, "Extreame_Weathers", DefDatabase<GameConditionDef>.AllDefs, null);
            TryAddDefs(result, "Normal_Weathers", DefDatabase<WeatherDef>.AllDefs, null);
            TryAddDefs(result, "All_Skills", DefDatabase<SkillDef>.AllDefs, null);
            TryAddDefs(result, "Pawn_Body_Parts", DefDatabase<BodyPartDef>.AllDefs, null);

            foreach (var list in result.Values)
                list.Sort((a, b) => string.Compare(a.DefName, b.DefName, StringComparison.Ordinal));

            return result;
        }

        private static void TryAddDefs<TDef>(Dictionary<string, List<GameObjectMeta>> result, string categoryId,
                                             IEnumerable<TDef> defs, Func<TDef, bool> filter) where TDef : Def
        {
            if (!result.TryGetValue(categoryId, out var list)) return;
            foreach (var def in defs)
            {
                if (filter != null && !filter(def)) continue;
                list.Add(MetaFromDef(def));
            }
        }

        private static List<(string id, Func<ThingDef, bool> predicate)> BuildItemPredicates()
        {
            var list = new List<(string, Func<ThingDef, bool>)>();
            void Add(string id, Func<ThingDef, bool> predicate) => list.Add((id, predicate));

            // Order matters: the first matching predicate wins.
            Add("Weapons_Tribal", t => t.IsWeapon && (t.techLevel == TechLevel.Neolithic || t.techLevel == TechLevel.Medieval));
            Add("Weapons_Industrial", t => t.IsWeapon && t.techLevel == TechLevel.Industrial);
            Add("Weapons_Spacer", t => t.IsWeapon && t.techLevel == TechLevel.Spacer);
            Add("Weapons_Archotech", t => t.IsWeapon && t.techLevel == TechLevel.Archotech);
            Add("Weapons_Ultra", t => t.IsWeapon && t.techLevel == TechLevel.Ultra);
            Add("Weapons_Other", t => t.IsWeapon);

            Add("Drugs", t => t.IsIngestible && t.IsDrug);
            Add("Medicine", t => t.IsMedicine);
            Add("Clothing", t => t.IsApparel);

            bool PlainFood(ThingDef t) => t.IsIngestible && !t.IsDrug && !t.IsMedicine;

            Add("Food_Meals", t => PlainFood(t) && HasCategory(t, "FoodMeals"));
            Add("Food_Raw_Plant", t => PlainFood(t) && (HasCategory(t, "PlantFoodRaw") || HasCategory(t, "PlantMatter")));
            Add("Food_Raw_Meat", t => PlainFood(t) && HasCategory(t, "MeatRaw"));
            Add("Food_Animal_Products", t => PlainFood(t) && (HasCategory(t, "EggsUnfertilized") || HasCategory(t, "AnimalProductRaw")));
            Add("Food_Raw_Special", t => PlainFood(t));

            Add("Metals", t => t.thingCategories != null && (HasStuffCategory(t, "Metallic") || HasCategory(t, "Metals")));
            Add("Stone", t => t.thingCategories != null && (HasStuffCategory(t, "Stony") || HasCategory(t, "StoneBlocks") || HasCategory(t, "Chunks")));
            Add("Textile_Leathers", t => t.thingCategories != null && (HasStuffCategory(t, "Leathery") || HasCategory(t, "Leathers")));
            Add("Textile_Fabrics", t => t.thingCategories != null && (HasStuffCategory(t, "Fabric") || HasCategory(t, "Textiles")));
            Add("Chemical_Raw", t => t.thingCategories != null &&
                (HasCategory(t, "Drugs") || HasCategory(t, "InorganicDrug") || HasCategory(t, "BodyPartsNatural") || HasCategory(t, "ResourcesRaw")));
            Add("Components", t => t.thingCategories != null && (HasCategory(t, "Manufactured") || HasCategory(t, "Components")));

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
    }

    /// <summary>Converts GameObjectMeta to/from the JSON stored in the DB's metadata column.</summary>
    public static class DefMetadata
    {
        private class Payload
        {
            public string Label;
            public string Description;
            public float MarketValue;
            public string TechLevel;
        }

        public static string ToJson(GameObjectMeta meta) => JsonConvert.SerializeObject(new Payload
        {
            Label = meta.Label,
            Description = meta.Description,
            MarketValue = meta.MarketValue,
            TechLevel = meta.TechLevel.ToString()
        });

        /// <summary>Rebuilds a GameObjectMeta from a DB row. Falls back to parsing "label: description" if there is no metadata.</summary>
        public static GameObjectMeta FromRow(string defName, string text, string metadataJson)
        {
            if (!string.IsNullOrEmpty(metadataJson))
            {
                try
                {
                    var p = JsonConvert.DeserializeObject<Payload>(metadataJson);
                    if (p != null)
                    {
                        var meta = new GameObjectMeta
                        {
                            DefName = defName,
                            Label = p.Label ?? defName,
                            Description = p.Description ?? string.Empty,
                            MarketValue = p.MarketValue
                        };
                        if (Enum.TryParse(p.TechLevel, out TechLevel tl)) meta.TechLevel = tl;
                        return meta;
                    }
                }
                catch { /* fall through to text parsing */ }
            }

            string label = defName, description = string.Empty;
            if (!string.IsNullOrEmpty(text))
            {
                int i = text.IndexOf(':');
                if (i > 0) { label = text.Substring(0, i).Trim(); description = text.Substring(i + 1).Trim(); }
                else label = text.Trim();
            }
            return new GameObjectMeta { DefName = defName, Label = label, Description = description };
        }
    }
}