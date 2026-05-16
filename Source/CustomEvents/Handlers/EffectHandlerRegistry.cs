using NAudio.Codecs;
using RimTelligence.Source.CustomEvents.Handlers.Implementations;
using RimTelligence.Source.CustomEvents.Handlers.Implementations.Environment;
using RimTelligence.Source.CustomEvents.Handlers.Implementations.ItemsRelated;
using RimTelligence.Source.CustomEvents.Handlers.Implementations.Pawns;
using RimTelligence.Source.CustomEvents.Handlers.Implementations.Pawns.Debuffs;
using System;
using System.Collections.Generic;

namespace RimTelligence.Source.CustomEvents.Handlers
{
    public static class EffectHandlerRegistry
    {
        private static readonly Dictionary<string, IEffectHandler> Handlers =
            new Dictionary<string, IEffectHandler>(StringComparer.OrdinalIgnoreCase);

        private static readonly (string type, string desc, float weight, EventAlignment align)[] _podVariants =
{
    ("ResourcePodCrash_WeaponTribal",     "tribal weapons fell from orbit",       3.0f, EventAlignment.Good),
    ("ResourcePodCrash_WeaponIndustrial", "industrial weapons pod crash",          0.5f, EventAlignment.Good),
    ("ResourcePodCrash_WeaponSpacer",     "spacer-tech weapons pod crash",         0.25f, EventAlignment.Good),
    ("ResourcePodCrash_WeaponArchotech",  "archotech weapon pod crash",            0.10f, EventAlignment.Good),
    ("ResourcePodCrash_WeaponUltra",      "ultra-tech weapon pod crash",           0.05f, EventAlignment.Good),
    ("ResourcePodCrash_WeaponOther",      "exotic weapon pod crash",               0.05f, EventAlignment.Good),
    ("ResourcePodCrash_FoodMeals",        "prepared meals dropped from orbit",     1.5f, EventAlignment.Good),
    ("ResourcePodCrash_FoodRawPlant",     "raw plant food pod crash",              1.5f, EventAlignment.Good),
    ("ResourcePodCrash_FoodRawMeat",      "raw meat pod crash",                    1.5f, EventAlignment.Good),
    ("ResourcePodCrash_FoodRawSpecial",   "special food pod crash",                0.8f, EventAlignment.Good),
    ("ResourcePodCrash_FoodAnimalProducts","animal products pod crash",            1.0f, EventAlignment.Good),
    ("ResourcePodCrash_Metals",           "metal shipment pod crash",              1.0f, EventAlignment.Good),
    ("ResourcePodCrash_Stone",            "stone blocks pod crash",                0.5f, EventAlignment.Good),
    ("ResourcePodCrash_TextileLeathers",  "leathers pod crash",                    1.0f, EventAlignment.Good),
    ("ResourcePodCrash_TextileFabrics",   "fabrics pod crash",                     1.0f, EventAlignment.Good),
    ("ResourcePodCrash_TextileWools",     "wools pod crash",                       0.8f, EventAlignment.Good),
    ("ResourcePodCrash_Chemicals",        "raw chemicals pod crash",               0.8f, EventAlignment.Good),
    ("ResourcePodCrash_Components",       "components pod crash",                  1.2f, EventAlignment.Good),
    ("ResourcePodCrash_Apparel",          "clothing pod crash",                    1.0f, EventAlignment.Good),
    ("ResourcePodCrash_Medicine",         "medicine pod crash",                    1.5f, EventAlignment.Good),
    ("ResourcePodCrash_Drugs",            "drugs pod crash",                       1.2f, EventAlignment.Good),
};

        private static readonly (string type, string desc, float weight, EventAlignment align)[] _foundVariants =
        {
    ("PawnFoundItem_WeaponTribal",        "pawn found a tribal weapon",            3.5f, EventAlignment.Good),
    ("PawnFoundItem_WeaponIndustrial",    "pawn found an industrial weapon",       0.5f, EventAlignment.Good),
    ("PawnFoundItem_WeaponSpacer",        "pawn found a spacer weapon",            0.25f, EventAlignment.Good),
    ("PawnFoundItem_WeaponArchotech",     "pawn found an archotech weapon",        0.1f, EventAlignment.Good),
    ("PawnFoundItem_WeaponUltra",         "pawn found an ultra-tech weapon",       0.05f, EventAlignment.Good),
    ("PawnFoundItem_FoodMeals",           "pawn found prepared food",              2.0f, EventAlignment.Good),
    ("PawnFoundItem_FoodRawPlant",        "pawn found raw plant food",             2.0f, EventAlignment.Good),
    ("PawnFoundItem_FoodRawMeat",         "pawn found raw meat",                   2.0f, EventAlignment.Good),
    ("PawnFoundItem_FoodRawSpecial",      "pawn found special food",               1.0f, EventAlignment.Good),
    ("PawnFoundItem_FoodAnimalProducts",  "pawn found animal products",            1.2f, EventAlignment.Good),
    ("PawnFoundItem_Metals",              "pawn found metal",                      1.2f, EventAlignment.Good),
    ("PawnFoundItem_Stone",               "pawn found stone blocks",               0.6f, EventAlignment.Good),
    ("PawnFoundItem_TextileLeathers",     "pawn found leather",                    1.2f, EventAlignment.Good),
    ("PawnFoundItem_TextileFabrics",      "pawn found fabric",                     1.2f, EventAlignment.Good),
    ("PawnFoundItem_TextileWools",        "pawn found wool",                       1.0f, EventAlignment.Good),
    ("PawnFoundItem_Chemicals",           "pawn found chemicals",                  1.0f, EventAlignment.Good),
    ("PawnFoundItem_Components",          "pawn found components",                 1.5f, EventAlignment.Good),
    ("PawnFoundItem_Apparel",             "pawn found clothing",                   1.2f, EventAlignment.Good),
    ("PawnFoundItem_Medicine",            "pawn found medicine",                   2.0f, EventAlignment.Good),
    ("PawnFoundItem_Drugs",               "pawn found drugs",                      1.5f, EventAlignment.Good),
};

        private static readonly (string type, string desc, float weight, EventAlignment align)[] _loseVariants =
        {
    ("LoseItems_WeaponTribal",            "tribal weapon lost",                    3.0f, EventAlignment.Bad),
    ("LoseItems_WeaponIndustrial",        "industrial weapon lost",                2.5f, EventAlignment.Bad),
    ("LoseItems_WeaponSpacer",            "spacer weapon lost",                    1.5f, EventAlignment.Bad),
    ("LoseItems_FoodMeals",              "prepared meals spoiled or stolen",       4.0f, EventAlignment.Bad),
    ("LoseItems_FoodRawPlant",           "raw plant food spoiled",                 3.5f, EventAlignment.Bad),
    ("LoseItems_FoodRawMeat",            "raw meat spoiled",                       3.5f, EventAlignment.Bad),
    ("LoseItems_Metals",                 "metals went missing",                    2.0f, EventAlignment.Bad),
    ("LoseItems_Stone",                  "stone blocks lost",                      1.0f, EventAlignment.Bad),
    ("LoseItems_TextileLeathers",        "leather lost",                           1.5f, EventAlignment.Bad),
    ("LoseItems_TextileFabrics",         "fabric lost",                            1.5f, EventAlignment.Bad),
    ("LoseItems_TextileWools",           "wool lost",                              1.2f, EventAlignment.Bad),
    ("LoseItems_Chemicals",              "chemicals lost",                         1.5f, EventAlignment.Bad),
    ("LoseItems_Components",             "components lost",                        2.0f, EventAlignment.Bad),
    ("LoseItems_Apparel",                "clothing lost",                          1.2f, EventAlignment.Bad),
    ("LoseItems_Medicine",               "medicine lost",                          3.0f, EventAlignment.Bad),
    ("LoseItems_Drugs",                  "drugs lost",                             2.0f, EventAlignment.Bad),
};

        static EffectHandlerRegistry()
        {

            foreach (var (type, desc, weight, align) in _podVariants)
                Register(new ResourcePodCrashHandler(type, desc, weight, align));

            foreach (var (type, desc, weight, align) in _foundVariants)
                Register(new PawnFoundItemsHandler(type, desc, weight, align));

            foreach (var (type, desc, weight, align) in _loseVariants)
                Register(new LoseItemsHandler(type, desc, weight, align));


            Register(new DamagePawnHandler());
            Register(new StartExtremeWeatherEvent());
            Register(new AnimalInsanityHandler());
            Register(new SelfTameHandler());
            Register(new SpawnAnimalsHandler());
            Register(new AddDiseaseHandler());
            Register(new MiracleHealChronicConditionHandler());
            Register(new MiracleHealDiseaseHandler());
            Register(new MiracleHealMissingPartHandler());
            Register(new ChangeWeatherHandler());
            Register(new MeteoriteImpactHandler());
            Register(new AddExpPointsHandler());
            Register(new LoseExpPointsHandler());
            Register(new CropBlightHandler());
            Register(new MoodBuffHandler());
            Register(new MoodDebuffHandler());
        }

        private static void Register(IEffectHandler handler)
        {
            Handlers[handler.EffectName] = handler;
        }

        public static IEffectHandler GetHandler(string effectType)
        {
            Handlers.TryGetValue(effectType, out var handler);
            return handler;
        }

        public static IEnumerable<IEffectHandler> GetAllHandlers()
        {
            return Handlers.Values;
        }
    }
}