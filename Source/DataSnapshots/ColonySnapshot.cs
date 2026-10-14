using RimWorld;
using System.Collections.Generic;

namespace RimTelligence.DataSnapshots
{
    public class ColonySnapshot
    {
        // identity
        public string colonyName;
        public int day;
        public string time;
        public string season;
        public string biome;

        // population
        public int colonistCount;
        public List<PawnSnapshot> colonists;

        // other inhibitants
        public int colonyAnimalCount;
        public List<AnimalSnapshot> colonyAnimals;
        public List<PawnSnapshot> prisoners;
        public List<PawnSnapshot> slaves;
        public List<PawnSnapshot> visitors;
        // add information about prisoners, slaves, visitors

        // threats & story
        public float wealthTotal;
        public float wealthItems;
        public float wealthBuildings;
        public float wealthPawns;
        public string currentTechLevel;
        public int daysSinceLastRaid;
        public List<string> activeConditions;
        public float totalNutrition;
        public bool isUnderAttck; // does not work for now 

        // environment
        public string currentWeather;
        public float temperature;
        public Dictionary<AnimalSnapshot, int> wildAnimals;
        public Dictionary<PlantSnapshot, int> cultivatedPlants;

        // resources
        public ResourceSnapshot colonyResources;

        // world & diplomacy 
        public Dictionary<string, int> factionRelations;
        public List<string> activeQuests;

    }

    public class PawnSnapshot
    {
        public string name;
        public string current_action;
        public List<string> queued_actions;
        public string current_location;
        public bool is_drafted;
        public string primary_weapon;
        public int age;
        public HealthSnapshot health;
        public List<string> traits;
        public string childhood;
        public string childhood_description;
        public string adulthood;
        public string adulthood_description;
        public List<string> skills;
        public PawnRelationsSnapshot relations;
        public PawnNeedsSnapshot needs;
    }

    public class PawnNeedsSnapshot
    {
        public float mood;       // 0–1
        public string mental_state;          // "Wandering", "Berserk", "Tantrum" null is normal
        public float food_level;             // 0-1
        public float rest_level;             // 0-1
        public float recreation_level;       // 0-1
    }

    public class PawnRelationsSnapshot
    {
        public List<string> lovers;
        public List<string> rivals;
        public string bonded_animal;
    }

    public class HealthSnapshot
    {
        public float overall_health;
        public bool is_downed;
        public float pain_level;

        public List<string> missing_body_parts;
        public List<string> injuries;
        public List<string> diseases; // name and description
        public List<string> implants;
        public List<string> chronic_conditions;  // scars, permanent
        public List<string> other;
    }
    public class ResourceSnapshot
    {
        public Dictionary<string, int> allResources;
    }

    public class EventSnapshot
    {
        public string title;
        public string description;

    }
    public class AnimalSnapshot 
    {
        public string name;
        public string description;

        
    }

    public class PlantSnapshot
    {
        public string name;
        public string description;
    }
}

