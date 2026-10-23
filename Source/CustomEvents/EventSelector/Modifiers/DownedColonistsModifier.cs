using RimTelligence.CustomEvents.Handlers;
using RimTelligence.DataSnapshots;
using System.Linq;

using Verse;

namespace RimTelligence.CustomEvents.EventSelector.Modifiers
{
    public class DownedColonistsModifier : IEventModifier
    {
        public float Modify(IEffect entry, ColonySnapshot snapshot)
        {
            if (!(snapshot.colonists?.Any(colonist => colonist.health?.is_downed == true) ?? false))
                return 1f;

            float multiplier = 1f;
            if (entry.Config.EffectName == "MiracleHealChronicCondition") multiplier *= 1.3f;
            if (entry.Config.EffectName == "MiracleHealMissingPart") multiplier *= 1.5f;
            if (entry.Config.EffectName == "AddDisease") multiplier *= 0.5f;
            if (entry.Config.EffectName == "DamagePawn") multiplier *= 0.3f;
            if (entry.Config.EffectName == "MoodBuff") multiplier *= 2f;
            if (entry.Config.EffectName == "MoodDebuff") multiplier *= 2f;

            return multiplier;
        }
    }
}