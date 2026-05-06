using RimTelligence.Source.CustomEvents;
using RimTelligence.Source.CustomEvents.Handlers;
using RimWorld.Planet;
using System;
using System.Linq;
using Verse;

internal class MiracleHealChronicConditionHandler : IEffectHandler
{
    public string EffectType => "MiracleHealChronicCondition";

    public string PromptDescription =>
        "- MiracleHealChronicCondition : defName = HediffDef defName (e.g. Carcinoma, ChemicalDamageSevere, Asthma), " +
        "targetPawn = colonist name";

    public string AditionalPromptInfo =>
        "Select only from the colonist's chronic_conditions list. " +
        "Use the exact defName string provided.";

    public GlobalTargetInfo Execute(LlmEffect effect, Map map)
    {
        Pawn target = map.mapPawns.FreeColonistsSpawned.FirstOrDefault(p =>
            p.LabelShort.Equals(effect.targetPawn, StringComparison.OrdinalIgnoreCase));

        if (target == null)
        {
            ModLogger.Warning($"MiracleHealChronicCondition: no spawned colonist found for '{effect.targetPawn}'.");
            return GlobalTargetInfo.Invalid;
        }

        var chronicDefNames = target.health.hediffSet.hediffs
            .Where(h => h.Visible && !(h is Hediff_MissingPart) && (h.IsPermanent() || h.def.chronic))
            .Select(h => h.def.defName);
        ModLogger.Message($"MiracleHealChronicCondition: {target.LabelShort} chronic conditions: [{string.Join(", ", chronicDefNames)}]");

        HediffDef hediffDef = DefDatabase<HediffDef>.GetNamed(effect.defName, errorOnFail: false);
        if (hediffDef == null)
        {
            ModLogger.Warning($"MiracleHealChronicCondition: HediffDef '{effect.defName}' not found.");
            return GlobalTargetInfo.Invalid;
        }

        Hediff existing = target.health.hediffSet.hediffs.FirstOrDefault(h =>
            h.def == hediffDef &&
            !(h is Hediff_MissingPart) &&
            (h.IsPermanent() || h.def.chronic));

        if (existing == null)
        {
            ModLogger.Warning($"MiracleHealChronicCondition: {target.LabelShort} has no chronic condition '{effect.defName}'.");
            return GlobalTargetInfo.Invalid;
        }

        target.health.RemoveHediff(existing);
        ModLogger.Message($"{target.LabelShort} was miraculously cured of {hediffDef.defName}.");

        return new GlobalTargetInfo(target);
    }

    public string GetSummary(LlmEffect effect) =>
        $"Cure chronic condition '{effect.defName}' on {effect.targetPawn}";
}