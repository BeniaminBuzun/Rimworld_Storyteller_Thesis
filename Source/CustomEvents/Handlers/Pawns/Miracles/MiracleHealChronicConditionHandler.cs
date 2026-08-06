using RimTelligence.CustomEvents;

namespace RimTelligence.CustomEvents.Handlers.Pawns.Miracles
{
using RimTelligence.CustomEvents.Handlers;
using RimWorld.Planet;
using System;
using System.Linq;
using Verse;

internal class MiracleHealChronicConditionHandler : IEffectHandler
{
    public string EffectName => "MiracleHealChronicCondition";

    public string NarrativeDescription => "A miraculous event where a specific colonist is instantly cured of a permanent or chronic health condition.";

    public string PromptStructure =>
        "- type = MiracleHealChronicCondition:\r\n" +
        "    defName = \"HediffDef name\" (e.g. Carcinoma, ChemicalDamageSevere, Asthma)\r\n" +
        "    count = 0\r\n" +
        "    defDescription = \"\"\r\n" +
        "    value = NONE\r\n" +
        "    targetPawn = \"colonist name\"";

    public string AditionalPromptStructureInfo =>
        "Select only from the colonist's chronic_conditions list. Use the exact defName string provided.";

    public float BaseWeight => 3f;

    public EventAlignment Alignment => EventAlignment.Good;

    public GlobalTargetInfo Execute(GameEffect effect, Map map)
    {
        Pawn target = map.mapPawns.FreeColonistsSpawned.FirstOrDefault(pawn =>
            pawn.LabelShort.Equals(effect.targetPawn, StringComparison.OrdinalIgnoreCase));

        if (target == null)
        {
            ModLogger.Warning($"MiracleHealChronicCondition: no spawned colonist found for '{effect.targetPawn}'.");
            return GlobalTargetInfo.Invalid;
        }

        var chronicDefNames = target.health.hediffSet.hediffs
            .Where(hediff => hediff.Visible && !(hediff is Hediff_MissingPart) && (hediff.IsPermanent() || hediff.def.chronic))
            .Select(hediff => hediff.def.defName);
        ModLogger.Message($"MiracleHealChronicCondition: {target.LabelShort} chronic conditions: [{string.Join(", ", chronicDefNames)}]");

        HediffDef hediffDef = DefDatabase<HediffDef>.GetNamed(effect.defName, errorOnFail: false);
        if (hediffDef == null)
        {
            ModLogger.Warning($"MiracleHealChronicCondition: HediffDef '{effect.defName}' not found.");
            return GlobalTargetInfo.Invalid;
        }

        Hediff existing = target.health.hediffSet.hediffs.FirstOrDefault(hediff =>
            hediff.def == hediffDef &&
            !(hediff is Hediff_MissingPart) &&
            (hediff.IsPermanent() || hediff.def.chronic));

        if (existing == null)
        {
            ModLogger.Warning($"MiracleHealChronicCondition: {target.LabelShort} has no chronic condition '{effect.defName}'.");
            return GlobalTargetInfo.Invalid;
        }

        target.health.RemoveHediff(existing);
        ModLogger.Message($"MiracleHealChronicCondition: {target.LabelShort} was cured of {hediffDef.defName}.");
        return new GlobalTargetInfo(target);
    }

    public string GetSummary(GameEffect effect) =>
        $"Chronic condition '{effect.defName}' cured on {effect.targetPawn}";
}
}