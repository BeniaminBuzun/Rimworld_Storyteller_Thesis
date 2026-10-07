using RimWorld;
using System.Collections.Generic;
using Verse;

namespace RimTelligence.AIStoryteller
{
    public class StorytellerComp_AI : StorytellerComp
    {
        private StorytellerCompProperties_AI Props => (StorytellerCompProperties_AI)props;
        private static readonly string INCIDENT_DEFNAME = "LlmEvent";

        //
        // This method is invoked by the Storyteller system 
        // once every 1000 ticks (in-game time unit)
        // 
        // Returned is a sequence of incident suggestions
        // for the Storyteller to fire
        //
        public override IEnumerable<FiringIncident> MakeIntervalIncidents(IIncidentTarget target)
        {
            if (ShouldFireIncidentAt(target))
            {
                var incidentParms = GenerateParms(Props.category, target);
                var incidentDef = DefDatabase<IncidentDef>.GetNamed(INCIDENT_DEFNAME);

                if (incidentDef != null)
                {
                    ModLogger.Message($"Requesting a new incident: {INCIDENT_DEFNAME}");

                    yield return new FiringIncident(incidentDef, this, incidentParms);
                }
                else
                {
                    ModLogger.Error($"Failed to locate {INCIDENT_DEFNAME} def in the DefDatabase");
                }
            }

            yield break;
        }

        //
        // Checks whether an LLM incident should
        // be fired by the Storyteller system
        //
        private bool ShouldFireIncidentAt(IIncidentTarget target)
        {
            bool modProperlyInitialized = AIStorytellerMod.Settings != null;
            bool isMap = (target as Map) != null;
            bool randomChance = Rand.Chance(Props.incidentChance);

            return modProperlyInitialized && isMap && randomChance;
        }
    }
}