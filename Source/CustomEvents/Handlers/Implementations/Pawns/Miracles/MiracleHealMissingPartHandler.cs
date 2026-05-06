using RimTelligence.Source.CustomEvents;
using RimTelligence.Source.CustomEvents.Handlers;
using RimWorld.Planet;
using System;
using System.Linq;
using Verse;

internal class MiracleHealMissingPartHandler : IEffectHandler
{
    public string EffectType => "MiracleHealMissingPart";

    public string PromptDescription =>
        "- MiracleHealMissingPart : defName = exact body part label (e.g. left hand, right foot), " +
        "targetPawn = colonist name";

    public string AditionalPromptInfo =>
        "Select only from the colonist's missing_body_parts list. " +
        "Use the exact defName string provided — do not guess or abbreviate.";


    public GlobalTargetInfo Execute(LlmEffect effect, Map map)
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
        ModLogger.Message($"{target.LabelShort} had their {missingPart.Part.LabelCap} restored.");

        return new GlobalTargetInfo(target);
    }

    public string GetSummary(LlmEffect effect) =>
        $"Restore missing part '{effect.defName}' on {effect.targetPawn}";
}