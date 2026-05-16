using RimTelligence.Source.CustomEvents;
using RimTelligence.Source.CustomEvents.Handlers;
using RimWorld.Planet;
using System;
using System.Linq;
using Verse;

internal class MiracleHealDiseaseHandler : IEffectHandler
{
    public string EffectName => "MiracleHealDisease";

    public string NarrativeDescription => "A miraculous event where a specific colonist is instantly cured of a disease.";

    public string PromptStructure =>
        "- type = MiracleHealDisease:\r\n" +
        "    defName = \"HediffDef name\" (e.g. Malaria, Plague, Flu)\r\n" +
        "    count = 0\r\n" +
        "    defDescription = \"\"\r\n" +
        "    value = NONE\r\n" +
        "    targetPawn = \"colonist name\"";

    public string AditionalPromptStructureInfo =>
        "Select only from the colonist's diseases list. Use the exact defName string provided.";

    public float BaseWeight => 3f;

    public EventAlignment Alignment => EventAlignment.Good;

    public GlobalTargetInfo Execute(GameEffect effect, Map map)
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
        ModLogger.Message($"MiracleHealDisease: {target.LabelShort}'s {hediffDef.defName} was healed.");
        return new GlobalTargetInfo(target);
    }

    public string GetSummary(GameEffect effect) =>
        $"Disease '{effect.defName}' healed on {effect.targetPawn}";
}

