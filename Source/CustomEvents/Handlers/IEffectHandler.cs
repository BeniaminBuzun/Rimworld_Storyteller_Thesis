using RimWorld.Planet;
using Verse;

namespace RimTelligence.Source.CustomEvents.Handlers
{
    public interface IEffectHandler
    {
        string EffectName { get; }
        // needed for llm prompt
        string NarrativeDescription { get; } // description for prompt, history what effect is doing
        string PromptStructure { get; } // so llm know what is the json structure
        string AditionalPromptStructureInfo { get; } // additional info what variables llm can choose from 

        // for event selection
        float BaseWeight { get; } // for event selection, higher = more common
        EventAlignment Alignment { get; }  // Good, Bad, Neutral

        GlobalTargetInfo Execute(GameEffect effect, Map map);

        string GetSummary(GameEffect effect);
    }
    public enum EventAlignment { Good, Bad, Neutral }
}