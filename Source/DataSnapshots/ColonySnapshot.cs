using System.Collections.Generic;

namespace RimTelligence.Source.DataSnapshots
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

        // threats & story
        public float wealthTotal;
        public float wealthItems;
        public float wealthBuildings;
        public float wealthPawns;
        public int daysSinceLastRaid;
        public List<string> activeConditions;
        public float totalNutrition;

        // environment
        public string currentWeather;
        public float temperature;
        public Dictionary<string, int> wildAnimals;
        public Dictionary<string, int> cultivatedPlants;

        // resources
        public ResourceSnapshot resources;

        public List<string> recentEventTitles;
    }

    public class PawnSnapshot
    {
        public string name;
        public string current_action;
        public List<string> queued_actions;
        public int age;
        public float mood;       // 0–1
        public HealthSnapshot health;     // 0–1
        public List<string> traits;
        public string childhood;
        public string adulthood;
        public List<string> skills;
    }

    public class HealthSnapshot 
    {
        public float overall_health;
        public bool is_downed;
        public float pain_level;

        public List<string> missing_body_parts;
        public List<string> injuries;
        public List<string> diseases;
        public List<string> implants;
        public List<string> chronic_conditions;  // scars, permanent
        public List<string> other;
    }
    public class ResourceSnapshot
    {
        public Dictionary<string, int> allResources;
    }
}



