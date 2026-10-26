using RimWorld;
using System.Collections.Generic;
using Verse;


using RimTelligence.CustomEvents;
using RimTelligence.CustomEvents.Workers;
using RimTelligence.Source.AIStoryteller;
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
                var stateTracker = Current.Game.GetComponent<StorytellerState>();

                //var incidentParms = GenerateParms(Props.category, target);
                //var incidentDef = DefDatabase<IncidentDef>.GetNamed(INCIDENT_DEFNAME);
                var incidentToFire = stateTracker.GetNextIncident(target, this);
                if (incidentToFire != null)
                {
                    ModLogger.Message($"Firing incident: {incidentToFire.def.defName}");

                    // Display a simple info dialog with an OK button
                    Find.WindowStack.Add(new Dialog_MessageBox(
                        text: incidentToFire.parms.customLetterText ?? "An LLM incident has occurred.",
                        title: incidentToFire.parms.customLetterLabel ?? "LLM Incident"
                    ));
                    //incidentDef.Worker.TryExecute(parms);
                    incidentToFire.def.Worker.TryExecute(incidentToFire.parms);
                    //yield return incidentToFire;
                }
                else
                {

                }
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
            //bool randomChance = Rand.Chance(Props.incidentChance);

            //return modProperlyInitialized && isMap && randomChance;
            return modProperlyInitialized && isMap;
        }
    }
}