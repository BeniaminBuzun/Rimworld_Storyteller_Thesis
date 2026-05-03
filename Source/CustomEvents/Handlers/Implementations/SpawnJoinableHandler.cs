using System.Linq;
using RimWorld;
using RimWorld.Planet;
using Verse;

namespace RimTelligence.Source.CustomEvents.Handlers
{
    public class SpawnJoinableHandler : IEffectHandler
    {
        public string EffectType => "SpawnJoinable";
        public string PromptDescription => "- SpawnJoinable  : defName = PawnKindDef, spawns a pawn with accept/reject dialog to join colony";

        public string AditionalPromptInfo => "";

        public GlobalTargetInfo Execute(LlmEffect effect, Map map)
        {
            PawnKindDef kind = DefDatabase<PawnKindDef>.GetNamed(effect.defName, errorOnFail: false);
            if (kind == null)
            {
                ModLogger.Warning($"SpawnJoinable: PawnKindDef '{effect.defName}' not found.");
                return GlobalTargetInfo.Invalid;
            }

            PawnGenerationRequest request = new PawnGenerationRequest(kind, Faction.OfAncients, forceGenerateNewPawn: true);
            Pawn pawn = PawnGenerator.GeneratePawn(request);

            IntVec3 spawnCell = CellFinder.RandomEdgeCell(map);
            GenSpawn.Spawn(pawn, spawnCell, map);
            ModLogger.Message($"Spawned joinable pawn: {pawn.Name} at {spawnCell}");

            string pawnName = pawn.Name.ToStringFull;
            string title = effect.defName ?? "A Stranger Arrives";
            string topSkills = string.Join(", ", pawn.skills.skills.OrderByDescending(s => s.levelInt).Take(3).Select(s => $"{s.def.defName} {s.levelInt}"));

            DiaNode node = new DiaNode(
                $"{pawnName} has appeared at the edge of your colony and wishes to join.\n\n" +
                $"Age: {pawn.ageTracker.AgeBiologicalYears}\n" +
                $"Skills: {topSkills}\n"
            );

            DiaOption accept = new DiaOption("Accept")
            {
                action = () =>
                {
                    pawn.SetFaction(Faction.OfPlayer);
                    ModLogger.Message($"Player accepted {pawnName} into colony.");
                    Messages.Message($"{pawnName} has joined your colony.", pawn, MessageTypeDefOf.PositiveEvent);
                },
                resolveTree = true
            };

            DiaOption reject = new DiaOption("Reject")
            {
                action = () =>
                {
                    pawn.DestroyOrPassToWorld();
                    ModLogger.Message($"Player rejected {pawnName}.");
                    Messages.Message($"{pawnName} has left.", MessageTypeDefOf.NeutralEvent);
                },
                resolveTree = true
            };

            node.options.Add(accept);
            node.options.Add(reject);
            Find.WindowStack.Add(new Dialog_NodeTree(node, title: title));

            return new GlobalTargetInfo(spawnCell, map);
        }

        public string GetSummary(LlmEffect effect) => $"  • A {effect.defName} arrived and wishes to join";
    }
}