using RimWorld;

namespace RimTelligence.Source.AIStoryteller
{
    public class StorytellerCompProperties_AI : StorytellerCompProperties
    {
        //
        // Stores the category of incidents
        // managed by the comp
        //
        public IncidentCategoryDef category;

        //
        // Chance of firing the incident
        // by the comp
        //
        public float incidentChance = 0.1f;

        //
        // Minimum number of days after which
        // the comp will be activated 
        // (inherited from StorytellerCompProperties)
        // 
        // public float minDaysPassed = -1f;
        //

        public StorytellerCompProperties_AI()
        {
            compClass = typeof(StorytellerComp_AI);
        }
    }
}
