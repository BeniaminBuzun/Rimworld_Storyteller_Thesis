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
        public List<string> wildAnimals;

        // resources
        public ResourceSnapshot resources;

        public List<string> recentIncidentsTitles;
        public List<string> recentRandomQuestsTitles;
        public List<string> recentRandomDecreesTitles;
    }

    public class PawnSnapshot
    {
        public string name;
        public int age;
        public float mood;       // 0–1
        public float health;     // 0–1
        public List<string> traits;
        public string childhood;
        public string adulthood;
        public List<string> skills;  // top skills only
        public List<string> hediffs; // injuries, diseases
    }
}

public class ResourceSnapshot
{
    public Dictionary<string, int> allResources;
}