using RimTelligence.DataSnapshots;
using RimWorld.Planet;
using System.Collections.Generic;
using Verse;

namespace RimTelligence.CustomEvents.Handlers
{
    public interface IEffect
    {

        EffectConfig Config { get; }
        GlobalTargetInfo Execute(GameEffect effect, Map map);
        string GetSummary(GameEffect effect);
        bool CanBeChoosen(ColonySnapshot snapshot);
    }

    public class EffectConfig
    {
        public string EffectName { get; set; }
        public string NarrativeDescription { get; set; }
        public string RelatedDefList { get; set; }
        public string SecondaryRelatedDefList { get; set; }
        public int DefCount { get; set; } = 1;
        public float BaseWeight { get; set; }
        public EventAlignment Alignment { get; set; }
        public bool IsDefRequired { get; set; }
        public bool UsesSeverity { get; set; }
        public bool UsesTargetPawn { get; set; }

        public List<GameObjectMeta> AllowedDefs { get; set; } = new List<GameObjectMeta>();
        public List<GameObjectMeta> SecondaryAllowedDefs { get; set; } = new List<GameObjectMeta>();

        public EffectConfig() { }

        public EffectConfig Clone()
        {
            return new EffectConfig
            {
                EffectName = EffectName,
                NarrativeDescription = NarrativeDescription,
                RelatedDefList = RelatedDefList,
                SecondaryRelatedDefList = SecondaryRelatedDefList,
                DefCount = DefCount,
                BaseWeight = BaseWeight,
                Alignment = Alignment,
                IsDefRequired = IsDefRequired,
                UsesSeverity = UsesSeverity,
                UsesTargetPawn = UsesTargetPawn,
                AllowedDefs = new List<GameObjectMeta>(AllowedDefs),
                SecondaryAllowedDefs = new List<GameObjectMeta>(SecondaryAllowedDefs)
            };
        }

        public EffectConfig(string effectName, string narrativeDescription, string relatedDefList,
            string secondaryRelatedDefList, int defCount, float baseWeight,
            EventAlignment alignment, bool isDefRequired, bool usesSeverity, bool usesTargetPawn)
        {
            EffectName = effectName;
            NarrativeDescription = narrativeDescription;
            RelatedDefList = relatedDefList;
            SecondaryRelatedDefList = secondaryRelatedDefList;
            DefCount = defCount;
            BaseWeight = baseWeight;
            Alignment = alignment;
            IsDefRequired = isDefRequired;
            UsesSeverity = usesSeverity;
            UsesTargetPawn = usesTargetPawn;
        }
    }

    public enum EventAlignment { Good, Bad, Neutral }
}