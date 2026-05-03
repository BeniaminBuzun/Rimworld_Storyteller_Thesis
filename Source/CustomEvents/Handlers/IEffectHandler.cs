using RimWorld.Planet;
using Verse;

namespace RimTelligence.Source.CustomEvents.Handlers
{
    public interface IEffectHandler
    {
        string EffectType { get; }

        string PromptDescription { get; }

        string AditionalPromptInfo { get; }

        GlobalTargetInfo Execute(LlmEffect effect, Map map);

        string GetSummary(LlmEffect effect);
    }
}