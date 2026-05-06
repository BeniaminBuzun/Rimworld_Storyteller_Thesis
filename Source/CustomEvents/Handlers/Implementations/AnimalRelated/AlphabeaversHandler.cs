using RimTelligence.Source.DataSnapshots;
using RimWorld;
using RimWorld.Planet;
using System;
using System.Linq;
using Verse;

namespace RimTelligence.Source.CustomEvents.Handlers
{
    public class AlphabeaversHandler : IEffectHandler
    {
        public string EffectType => "Alphabeavers";
        public string PromptDescription => "- Alphabeavers   : count = how many spawn";

        public string AditionalPromptInfo => "";

        public GlobalTargetInfo Execute(LlmEffect effect, Map map)
        {
            PawnKindDef beaverKind = PawnKindDef.Named("Alphabeaver");
            if (beaverKind == null)
            {
                ModLogger.Warning("Alphabeavers: PawnKindDef 'Alphabeaver' not found.");
                return GlobalTargetInfo.Invalid;
            }

            int count = effect.count > 0 ? effect.count : 3;
            IntVec3 spawnCell = CellFinder.RandomEdgeCell(map);
            Pawn lastBeaver = null;

            for (int i = 0; i < count; i++)
            {
                PawnGenerationRequest request = new PawnGenerationRequest(beaverKind, null);
                Pawn beaver = PawnGenerator.GeneratePawn(request);
                GenSpawn.Spawn(beaver, CellFinder.RandomEdgeCell(map), map);
                lastBeaver = beaver;
                ModLogger.Message($"Spawned alphabeaver at {spawnCell}.");
            }

            return lastBeaver != null ? new GlobalTargetInfo(lastBeaver) : GlobalTargetInfo.Invalid;
        }

        public string GetSummary(LlmEffect effect) => $"{effect.count} alphabeaver(s) spawned";
    }
}