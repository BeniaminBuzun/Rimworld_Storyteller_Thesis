using RimWorld;
using RimWorld.Planet;
using Verse;

namespace RimTelligence.Source.CustomEvents.Handlers
{
    public class SpawnPawnsHandler : IEffectHandler
    {
        public string EffectType => "SpawnPawns";
        public string PromptDescription => "- SpawnPawns     : defName = PawnKindDef (e.g. Drifter, Pirate), count = how many";

        public string AditionalPromptInfo => "";

        public GlobalTargetInfo Execute(LlmEffect effect, Map map)
        {
            PawnKindDef kind = DefDatabase<PawnKindDef>.GetNamed(effect.defName, errorOnFail: false);
            if (kind == null)
            {
                ModLogger.Warning($"SpawnPawns: PawnKindDef '{effect.defName}' not found.");
                return GlobalTargetInfo.Invalid;
            }

            IntVec3 spawnCell = CellFinder.RandomEdgeCell(map);
            Pawn lastPawn = null;

            for (int i = 0; i < effect.count; i++)
            {
                PawnGenerationRequest request = new PawnGenerationRequest(kind, Faction.OfPlayer);
                lastPawn = PawnGenerator.GeneratePawn(request);
                GenSpawn.Spawn(lastPawn, spawnCell, map);
                ModLogger.Message($"Spawned pawn: {lastPawn.Name} at {spawnCell}");
            }

            return lastPawn != null ? new GlobalTargetInfo(lastPawn) : GlobalTargetInfo.Invalid;
        }

        public string GetSummary(LlmEffect effect) => $"  • {effect.count} {effect.defName} spawned at map edge";
    }
}