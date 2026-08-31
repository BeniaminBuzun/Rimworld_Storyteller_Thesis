using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;

namespace RimTelligence.DataSnapshots
{
    public static class PawnSnapshotCapturer
    {
        //Pawn
        public static List<PawnSnapshot> CaptureColonists(List<Pawn> pawns)
        {
            if (pawns == null)
            {
                ModLogger.Error("PawnSnapshotCapturer.CaptureColonists: pawn list is null; returning empty.");
                return new List<PawnSnapshot>();
            }

            ModLogger.Debug("PawnSnapshotCapturer.CaptureColonists: capturing " + pawns.Count + " colonist(s).");
            return pawns.Select(CaptureColonist).ToList();
        }

        public static PawnSnapshot CaptureColonist(Pawn pawn)
        {
            if (pawn == null)
            {
                ModLogger.Error("PawnSnapshotCapturer.CaptureColonist: pawn is null; returning empty snapshot.");
                return new PawnSnapshot();
            }

            ModLogger.Debug("PawnSnapshotCapturer.CaptureColonist: capturing '" +
                (pawn.Name?.ToStringShort ?? "Unknown") + "'.");

            return new PawnSnapshot
            {
                name = pawn.Name?.ToStringShort ?? "Unknown",
                age = pawn.ageTracker?.AgeBiologicalYears ?? 0,
                health = CaptureHealth(pawn),
                traits = CaptureTraits(pawn),
                childhood = pawn.story?.Childhood?.title ?? "Unknown",
                adulthood = pawn.story?.Adulthood?.title ?? "None",
                skills = CaptureSkills(pawn),
                current_action = GetCurrentAction(pawn),
                queued_actions = GetCurrentActionQueue(pawn),
                current_location = GetRoomName(pawn),
                needs = GetPawnNeeds(pawn),
                relations = CaptureRelations(pawn),
                is_drafted = pawn.Drafted,
                primary_weapon = pawn.equipment?.Primary?.def?.defName ?? "None"

            };
        }

        private static List<string> CaptureTraits(Pawn pawn)
        {
            try
            {
                return pawn.story?.traits?.allTraits
                    ?.Select(trait => trait.def.defName)
                    .ToList()
                    ?? new List<string>();
            }
            catch (Exception) { return new List<string>(); }
        }

    private static string GetRoomName(Pawn pawn)
    {
        Room room = pawn.GetRoom();

        // If the room is null or it's considered outside (no roof / map edge)
        if (room == null || room.PsychologicallyOutdoors)
        {
            return "Outside";
        }

        // If the room has a specific designated role (Dining room, Bedroom, Hospital, etc.)
        if (room.Role != null && room.Role != RoomRoleDefOf.None)
        {
            // Use .LabelCap for capitalized (e.g. "Dining room"), or .label for lowercase
            return room.Role.LabelCap;
        }

        // Enclosed room with no specific purpose yet (just walls and a roof)
        return "Empty room";
    }

    private static PawnRelationsSnapshot CaptureRelations(Pawn pawn)
    {
        try
        {
                return new PawnRelationsSnapshot
                {
                    rivals = pawn.Map?.mapPawns?.FreeColonistsSpawned
                        ?.Where(other => other != pawn && pawn.relations?.OpinionOf(other) < -20)
                        ?.Select(other => other.Name?.ToStringShort ?? "Unknown")
                        ?.ToList() ?? new List<string>(),

                    lovers = pawn.relations?.DirectRelations
                        ?.Where(rel => rel.def == PawnRelationDefOf.Lover ||
                                       rel.def == PawnRelationDefOf.Spouse ||
                                       rel.def == PawnRelationDefOf.Fiance)
                        ?.Select(rel => rel.otherPawn?.Name?.ToStringShort ?? "Unknown")
                        ?.ToList() ?? new List<string>(),

                    bonded_animal = pawn.relations?.DirectRelations
                        ?.Where(rel => rel.def == PawnRelationDefOf.Bond)
                        ?.Select(rel => rel.otherPawn?.Name?.ToStringShort ?? rel.otherPawn?.LabelShort)
                        ?.FirstOrDefault() ?? "None"
                };
        }
        catch (Exception) { return new PawnRelationsSnapshot(); }
    }

    private static List<string> CaptureSkills(Pawn pawn)
        {
            try
            {
                return pawn.skills?.skills
                    ?.OrderByDescending(skill => skill.levelInt)
                    .Take(3)
                    .Select(skill => $"{skill.def.defName}:{skill.levelInt}")
                    .ToList()
                    ?? new List<string>();
            }
            catch (Exception) { return new List<string>(); }
        }

        private static HealthSnapshot CaptureHealth(Pawn pawn)
        {
            try
            {
                HediffSet headifset = pawn.health.hediffSet;
                return new HealthSnapshot
                {
                    overall_health = pawn.health.summaryHealth.SummaryHealthPercent,
                    is_downed = pawn.health.Downed,
                    pain_level = headifset.PainTotal,

                    missing_body_parts = GetMissingParts(headifset),
                    injuries = GetInjuries(headifset),
                    diseases = GetDiseases(headifset),
                    implants = GetImplants(headifset),
                    chronic_conditions = GetChronicConditions(headifset),
                    other = GetOther(headifset),

                };
            }
            catch (Exception) { return new HealthSnapshot(); }
        }

        private static PawnNeedsSnapshot GetPawnNeeds(Pawn pawn)
        {
            try
            {
                return new PawnNeedsSnapshot
                {
                    mood = pawn.needs?.mood?.CurLevelPercentage ?? 0f,
                    mental_state = pawn.MentalStateDef?.defName ?? "None",
                    food_level = pawn.needs?.food?.CurLevelPercentage ?? 0f,
                    rest_level = pawn.needs?.rest?.CurLevelPercentage ?? 0f,
                    recreation_level = pawn.needs?.joy?.CurLevelPercentage ?? 0f,
                };
            }
            catch (Exception) { return new PawnNeedsSnapshot(); }
        }

        private static List<string> GetMissingParts(HediffSet hs)
        {
            var missingParts = hs.hediffs
                .OfType<Hediff_MissingPart>()
                .Select(hediff => hediff.Part)
                .Where(part => part != null)
                .ToList();

            return missingParts
                .Where(part => !HasMissingAncestor(part, missingParts))
                .Select(part => part.LabelCap.ToString())
                .ToList();
        }

        private static bool HasMissingAncestor(BodyPartRecord part, List<BodyPartRecord> missingParts)
        {
            var parent = part.parent;
            while (parent != null)
            {
                if (missingParts.Contains(parent))
                    return true;

                parent = parent.parent;
            }
            return false;
        }

        private static List<string> GetInjuries(HediffSet hs)
        {
            return hs.hediffs
                .OfType<Hediff_Injury>()
                .Where(hediff => hediff.Visible && !hediff.IsPermanent())
                .Select(hediff => hediff.def.defName)
                .ToList();
        }

        private static List<string> GetChronicConditions(HediffSet hs)
        {
            return hs.hediffs
                .Where(hediff => hediff.Visible && (hediff.IsPermanent() || hediff.def.chronic))
                .Select(hediff => hediff.def.defName)
                .ToList();
        }

        private static List<string> GetDiseases(HediffSet hs)
        {
            return hs.hediffs
                .Where(hediff => hediff.Visible
                    && !(hediff is Hediff_Injury)
                    && !(hediff is Hediff_MissingPart)
                    && !(hediff is Hediff_Addiction)
                    && !(hediff is Hediff_AddedPart)
                    && !(hediff is Hediff_Implant)
                    && !hediff.IsPermanent()
                    && !hediff.def.chronic
                    && hediff.def.isBad)
                .Select(hediff => hediff.def.defName)
                .ToList();
        }

        private static List<string> GetImplants(HediffSet hs)
        {
            return hs.hediffs
                .Where(hediff => hediff.Visible && (hediff is Hediff_AddedPart || hediff is Hediff_Implant))
                .Select(hediff => hediff.def.defName)
                .ToList();
        }

        private static List<string> GetOther(HediffSet hs)
        {
            return hs.hediffs
                .Where(hediff => hediff.Visible
                    && !hediff.def.isBad
                    && !(hediff is Hediff_AddedPart)
                    && !(hediff is Hediff_Implant))
                .Select(hediff => hediff.def.defName)
                .ToList();
        }

        private static string GetCurrentAction(Pawn pawn) {
            string currentAction = "Idle";
            if (pawn.jobs != null && pawn.jobs.curJob != null)
            {
                try
                {
                    // GetReport() generates the human-readable text you see in the bottom-left corner of the UI
                    currentAction = pawn.jobs.curJob.GetReport(pawn).CapitalizeFirst();
                }
                catch
                {
                    currentAction = "Doing something obscure";
                }
            }
            return currentAction;
            
        }
        private static List<string> GetCurrentActionQueue(Pawn pawn)
        {
            List<string> queuedActions = new List<string>();
            if (pawn.jobs != null && pawn.jobs.jobQueue != null)
            {
                foreach (Verse.AI.QueuedJob qj in pawn.jobs.jobQueue)
                {
                    if (qj.job != null)
                    {
                        try
                        {
                            queuedActions.Add(qj.job.GetReport(pawn).CapitalizeFirst());
                        }
                        catch
                        {
                            queuedActions.Add("Unknown queued action");
                        }
                    }
                }
            }
            return queuedActions;
        }
    }
}