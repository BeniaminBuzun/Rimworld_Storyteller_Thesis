using RimTelligence.Source.CustomEvents;
using RimTelligence.Source.CustomEvents.Handlers;
using RimWorld.Planet;
using System;
using System.Linq;
using Verse;

internal class MiracleHealDiseaseHandler : IEffectHandler
{
    public string EffectType => "MiracleHealDisease";

    public string PromptDescription =>
        "- MiracleHealDisease : defName = HediffDef defName (e.g. Malaria, Plague, Flu), " +
        "targetPawn = colonist name";

    public string AditionalPromptInfo =>
        "Select only from the colonist's diseases list. " +
        "Use the exact defName string provided.";

    public GlobalTargetInfo Execute(LlmEffect effect, Map map)
    {
        Pawn target = map.mapPawns.FreeColonistsSpawned.FirstOrDefault(p =>
            p.LabelShort.Equals(effect.targetPawn, StringComparison.OrdinalIgnoreCase));

        if (target == null)
        {
            ModLogger.Warning($"MiracleHealDisease: no spawned colonist found for '{effect.targetPawn}'.");
            return GlobalTargetInfo.Invalid;
        }

        HediffDef hediffDef = DefDatabase<HediffDef>.GetNamed(effect.defName, errorOnFail: false);
        if (hediffDef == null)
        {
            ModLogger.Warning($"MiracleHealDisease: HediffDef '{effect.defName}' not found.");
            return GlobalTargetInfo.Invalid;
        }

        Hediff existing = target.health.hediffSet.hediffs.FirstOrDefault(h =>
            h.def == hediffDef &&
            !(h is Hediff_MissingPart) &&
            !h.IsPermanent() &&
            !h.def.chronic);

        if (existing == null)
        {
            ModLogger.Warning($"MiracleHealDisease: {target.LabelShort} has no active disease '{effect.defName}'.");
            return GlobalTargetInfo.Invalid;
        }

        target.health.RemoveHediff(existing);
        ModLogger.Message($"{target.LabelShort}'s {hediffDef.defName} was healed.");

        return new GlobalTargetInfo(target);
    }

    public string GetSummary(LlmEffect effect) =>
        $"Heal disease '{effect.defName}' on {effect.targetPawn}";
}