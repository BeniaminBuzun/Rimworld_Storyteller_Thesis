using Newtonsoft.Json;
using RimWorld;
using System;
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
        public List<GameObjectMeta> colonyAnimals;
        public List<PawnSnapshot> prisoners;
        public List<PawnSnapshot> slaves;
        public List<PawnSnapshot> visitors;

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

        public List<CountedMeta> wildAnimals;
        public List<CountedMeta> cultivatedPlants;

        // resources
        public List<CountedMeta> colonyResources;

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

        public List<GameObjectMeta> missing_body_parts;
        public List<GameObjectMeta> injuries;
        public List<GameObjectMeta> diseases;
        public List<GameObjectMeta> implants;
        public List<GameObjectMeta> chronic_conditions;  // scars, permanent
        public List<GameObjectMeta> other;
    }

    public class EventSnapshot
    {
        public string title;
        public string description;

    }
}

