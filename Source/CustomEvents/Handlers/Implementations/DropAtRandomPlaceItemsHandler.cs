using System;
using RimWorld;
using RimWorld.Planet;
using Verse;

namespace RimTelligence.Source.CustomEvents.Handlers
{
    public class DropAtRandomPlaceItemsHandler : IEffectHandler
    {
        public string EffectType => "DropAtRandomPlaceItems";
        public string PromptDescription => "- DropAtRandomPlaceItems      : defName = ThingDef (Silver, Wort, etc. ), count = amount";

        public string AditionalPromptInfo => "select only from these itmes (Silver, Wort,PsychicAnimalPulser, PsychicSoothePulser, MechSerumHealer, MechSerumResurrector, TechprofSubpersonaCore, ThrumboHorn, ElephantTusk, Kibble, Pemmican, Chocolate, Milk, InsectJelly, Skull, EggChickenUnfertilized, EggChickenFertilized, EggCobraFertilized, EggCobraUnfertilized, EggIguanaFertilized, EggIguanaUnfertilized, EggTortoiseFertilized, EggTortoiseUnfertilized, EggCassowaryFertilized, EggCassowaryUnfertilized, EggEmuFertilized, EggEmuUnfertilized, EggOstrichFertilized, EggOstrichUnfertilized, EggTurkeyFertilized, EggTurkeyUnfertilized, EggDuckUnfertilized, EggDuckFertilized, EggGooseUnfertilized, EggGooseFertilized, MedicineHerbal, MedicineIndustrial, MedicineUltratech, ComponentIndustrial, ComponentSpacer, Neutroamine, Chemfuel, ReinforcedBarrel, RawPotatoes, RawFungus, RawRice, RawAgave, RawCorn, RawBerries, Hay, RawHops, PsychoidLeaves, SmokeleafLeaves, Shell_HighExplosive, Shell_Incendiary, Shell_EMP, Shell_Smoke, Shell_Firefoam, Shell_AntigrainWarhead, Gold, Steel, Plasteel, WoodLog, Uranium, Jade, Cloth, Synthread, DevilstrandCloth, Hyperweave, WoolSheep, WoolAlpaca, WoolMegasloth, WoolMuffalo, WoolBison, Leather_Plain, Leather_Dog, Leather_Wolf, Leather_Panthera, Leather_Camel, Leather_Bluefur, Leather_Bear, Leather_GuineaPig, Leather_Human, Leather_Pig, Leather_Light, Leather_Bird, Leather_Chinchilla, Leather_Fox, Leather_Lizard, Leather_Elephant, Leather_Heavy, Leather_Rhinoceros, Leather_Thrumbo, Leather_Patch, Apparel_PsychicShockLance, Apparel_PsychicInsanityLance, BlocksSandstone, BlocksGranite, BlocksLimestone, BlocksSlate, BlocksMarble, Meat_Bear_Grizzly, Meat_Cassowary, Meat_Cougar, Meat_Panther, Meat_Lynx, Meat_Cat, Meat_YorkshireTerrier, Meat_GuineaPig, Meat_LabradorRetriever, Meat_Husky, Meat_Monkey, Meat_Chicken, Meat_Duck, Meat_Turkey, Meat_Cow, Meat_Boomalope, Meat_Muffalo, Meat_Bison, Meat_Dromedary, Meat_Goat, Meat_Elk, Meat_Yak, Meat_Caribou, Meat_Horse, Meat_Donkey, Meat_Elephant, Meat_Rhinoceros, Meat_Hare, Meat_Megaspider, Meat_Thrumbo, Meat_Cobra, Meat_Tortoise, Meat_Alphabeaver, Meat_Pig, Meat_Ibex, Meat_Deer, Meat_Gazelle, Meat_Chinchilla, Meat_Sheep, Meat_Alpaca, Meat_Megasloth, Meat_Capybara, Meat_Squirrel, Meat_Rat, Meat_Boomrat, Meat_Raccoon, Meat_Iguana, Meat_Warg, Meat_Wolf_Timber, Meat_Fox_Fennec, Meat_Human, Beer, Ambrosia, GoJuice, Luciferium, Penoxycyline, Flake, PsychiteTea, Yayo, SmokeleafJoint, WakeUp, MealSurvivalPack, MealNutrientPaste, MealSimple, MealFine, MealFine_Veg, MealFine_Meat, MealLavish, MealLavish_Veg, MealLavish_Meat, Dye)";

        public GlobalTargetInfo Execute(LlmEffect effect, Map map)
        {
            ThingDef def = DefDatabase<ThingDef>.GetNamed(effect.defName, errorOnFail: false);
            if (def == null)
            {
                ModLogger.Warning($"DropAtRandomPlaceItems: ThingDef '{effect.defName}' not found.");
                return GlobalTargetInfo.Invalid;
            }

            Thing item = ThingMaker.MakeThing(def);
            item.stackCount = Math.Min(effect.count, def.stackLimit);

            IntVec3 dropCell = DropCellFinder.RandomDropSpot(map);
            GenPlace.TryPlaceThing(item, dropCell, map, ThingPlaceMode.Near);
            ModLogger.Message($"Gave {item.stackCount}x {def.defName} at {dropCell}");

            return new GlobalTargetInfo(dropCell, map);
        }

        public string GetSummary(LlmEffect effect) => $"  • {effect.count}x {effect.defName} dropped at trade spot";
    }
}