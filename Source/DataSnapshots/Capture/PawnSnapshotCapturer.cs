using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;

namespace RimTelligence.Source.DataSnapshots.Capture
{
    public static class PawnSnapshotCapturer
    {
        //Pawn

        public static List<PawnSnapshot> CaptureColonists(List<Pawn> pawns)
        {
            return pawns.Select(CaptureColonist).ToList();
        }

        public static PawnSnapshot CaptureColonist(Pawn pawn)
        {
            return new PawnSnapshot
            {
                name = pawn.Name?.ToStringShort ?? "Unknown",
                age = pawn.ageTracker?.AgeBiologicalYears ?? 0,
                mood = pawn.needs?.mood?.CurLevelPercentage ?? 0f,
                health = CaptureHealth(pawn),
                traits = CaptureTraits(pawn),
                childhood = pawn.story?.Childhood?.title ?? "Unknown",
                adulthood = pawn.story?.Adulthood?.title ?? "None",
                skills = CaptureSkills(pawn),
                current_action = GetCurrentAction(pawn),
                queued_actions = GetCurrentActionQueue(pawn)
            };
        }

        private static List<string> CaptureTraits(Pawn pawn)
        {
            try
            {
                return pawn.story?.traits?.allTraits
                    ?.Select(t => t.def.defName)
                    .ToList()
                    ?? new List<string>();
            }
            catch (Exception) { return new List<string>(); }
        }

        private static List<string> CaptureSkills(Pawn pawn)
        {
            try
            {
                return pawn.skills?.skills
                    ?.OrderByDescending(s => s.levelInt)
                    .Take(3)
                    .Select(s => $"{s.def.defName}:{s.levelInt}")
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
        private static List<string> GetMissingParts(HediffSet hs)
        {
            var missingParts = hs.hediffs
                .OfType<Hediff_MissingPart>()
                .Select(h => h.Part)
                .Where(p => p != null)
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
                .Where(h => h.Visible && !h.IsPermanent())
                .Select(h => h.def.defName)
                .ToList();
        }

        private static List<string> GetChronicConditions(HediffSet hs)
        {
            return hs.hediffs
                .Where(h => h.Visible && (h.IsPermanent() || h.def.chronic))
                .Select(h => h.def.defName)
                .ToList();
        }

        private static List<string> GetDiseases(HediffSet hs)
        {
            return hs.hediffs
                .Where(h => h.Visible
                    && !(h is Hediff_Injury)
                    && !(h is Hediff_MissingPart)
                    && !(h is Hediff_Addiction)
                    && !(h is Hediff_AddedPart)
                    && !(h is Hediff_Implant)
                    && !h.IsPermanent()
                    && !h.def.chronic
                    && h.def.isBad)
                .Select(h => h.def.defName)
                .ToList();
        }

        private static List<string> GetImplants(HediffSet hs)
        {
            return hs.hediffs
                .Where(h => h.Visible && (h is Hediff_AddedPart || h is Hediff_Implant))
                .Select(h => h.def.defName)
                .ToList();
        }

        private static List<string> GetOther(HediffSet hs)
        {
            return hs.hediffs
                .Where(h => h.Visible
                    && !h.def.isBad
                    && !(h is Hediff_AddedPart)
                    && !(h is Hediff_Implant))
                .Select(h => h.def.defName)
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
