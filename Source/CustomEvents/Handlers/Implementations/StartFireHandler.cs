using RimWorld;
using RimWorld.Planet;
using Verse;

namespace RimTelligence.Source.CustomEvents.Handlers
{
    public class StartFireHandler : IEffectHandler
    {
        public string EffectType => "StartFire";
        public string PromptDescription => "- StartFire      : count = number of fires";

        public string AditionalPromptInfo => "";

        public GlobalTargetInfo Execute(LlmEffect effect, Map map)
        {
            IntVec3 firstFire = IntVec3.Invalid;

            for (int i = 0; i < effect.count; i++)
            {
                IntVec3 cell = CellFinder.RandomCell(map);
                if (cell.Standable(map))
                {
                    FireUtility.TryStartFireIn(cell, map, 1f, null);
                    ModLogger.Message($"Fire started at {cell}");
                    if (!firstFire.IsValid) firstFire = cell;
                }
            }

            return firstFire.IsValid ? new GlobalTargetInfo(firstFire, map) : GlobalTargetInfo.Invalid;
        }

        public string GetSummary(LlmEffect effect) => $"  • {effect.count} fire(s) started on the map";
    }
}