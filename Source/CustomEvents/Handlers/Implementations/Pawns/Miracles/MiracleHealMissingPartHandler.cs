using RimTelligence.Source.CustomEvents;
using RimTelligence.Source.CustomEvents.Handlers;
using RimWorld.Planet;
using System;
using System.Linq;
using Verse;

internal class MiracleHealMissingPartHandler : IEffectHandler
{
    public string EffectName => "MiracleHealMissingPart";

    public string NarrativeDescription => "A miraculous event where a specific colonist regrows a missing body part.";

    public string PromptStructure =>
        "- type = MiracleHealMissingPart:\r\n" +
        "    defName = \"body part label\" (e.g. left hand, right foot)\r\n" +
        "    count = 0\r\n" +
        "    defDescription = \"\"\r\n" +
        "    value = NONE\r\n" +
        "    targetPawn = \"colonist name\"";

    public string AditionalPromptStructureInfo =>
        "Select only from the colonist's missing_body_parts list. Use the exact label string provided — do not guess or abbreviate.";

    public float BaseWeight => 3f;

    public EventAlignment Alignment => EventAlignment.Good;

    public GlobalTargetInfo Execute(GameEffect effect, Map map)
    {
        Pawn target = map.mapPawns.FreeColonistsSpawned.FirstOrDefault(p =>
            p.LabelShort.Equals(effect.targetPawn, StringComparison.OrdinalIgnoreCase));

        if (target == null)
        {
            ModLogger.Warning($"MiracleHealMissingPart: no spawned colonist found for '{effect.targetPawn}'.");
            return GlobalTargetInfo.Invalid;
        }

        Hediff_MissingPart missingPart = target.health.hediffSet.hediffs
            .OfType<Hediff_MissingPart>()
            .FirstOrDefault(h =>
                h.Part != null &&
                h.Part.LabelCap.ToString().Equals(effect.defName, StringComparison.OrdinalIgnoreCase));

        if (missingPart == null)
        {
            ModLogger.Warning($"MiracleHealMissingPart: {target.LabelShort} has no missing part '{effect.defName}'.");
            return GlobalTargetInfo.Invalid;
        }

        target.health.RestorePart(missingPart.Part);
        ModLogger.Message($"MiracleHealMissingPart: {target.LabelShort}'s {missingPart.Part.LabelCap} was restored.");
        return new GlobalTargetInfo(target);
    }

    public string GetSummary(GameEffect effect) =>
        $"Missing part '{effect.defName}' restored on {effect.targetPawn}";
}

