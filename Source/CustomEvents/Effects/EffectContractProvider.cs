using RimTelligence.DataSnapshots;
using RimTelligence.Source.CustomEvents.Handlers;
using System;
using System.Collections.Generic;
using System.Linq;

namespace RimTelligence.CustomEvents.Handlers
{
    public static class EffectContractProvider
    {
        private const string DiseaseListId = "ColonistsDiseaseList";
        private const string ChronicConditionListId = "ColonistsChronicConditionList";
        private const string MissingBodyPartListId = "ColonistsMissingBodyPartsList";
        private const string WildAnimalListId = "WildAnimals";
        private const string ColonyResourcesListId = "ColonyResources";

        public static EffectConfig GetContract(IEffect effect)
        {
            return GetContract(effect, null);
        }

        public static EffectConfig GetContract(IEffect effect, ColonySnapshot snapshot)
        {
            if (effect?.Config == null)
                return new EffectConfig();

            EffectConfig config = effect.Config;
            List<GameObjectMeta> primary = new List<GameObjectMeta>();
            List<GameObjectMeta> secondary = new List<GameObjectMeta>();

            if (config.IsDefRequired)
            {
                string primaryList = config.RelatedDefList;
                if (string.IsNullOrEmpty(primaryList) && (string.Equals(config.EffectName, "ResourcePodCrash", StringComparison.OrdinalIgnoreCase) || string.Equals(config.EffectName, "PawnFoundItems", StringComparison.OrdinalIgnoreCase)))
                {
                    primaryList = ItemCategoryRegistry.DrawRandomCategory();
                }

                primary = ResolveDefs(primaryList, snapshot);
                if (!string.IsNullOrEmpty(config.SecondaryRelatedDefList))
                    secondary = ResolveDefs(config.SecondaryRelatedDefList, snapshot);
            }

            return new EffectConfig
            {
                EffectName = config.EffectName,
                NarrativeDescription = config.NarrativeDescription,
                RelatedDefList = config.RelatedDefList,
                SecondaryRelatedDefList = config.SecondaryRelatedDefList,
                BaseWeight = config.BaseWeight,
                Alignment = config.Alignment,
                DefCount = config.DefCount,
                IsDefRequired = config.IsDefRequired,
                UsesSeverity = config.UsesSeverity,
                UsesTargetPawn = config.UsesTargetPawn,
                AllowedDefs = primary,
                SecondaryAllowedDefs = secondary
            };
        }

        public static List<EffectConfig> GetContracts(IEnumerable<IEffect> effects)
        {
            List<EffectConfig> contracts = new List<EffectConfig>();
            if (effects == null)
                return contracts;

            foreach (IEffect effect in effects)
                contracts.Add(GetContract(effect));

            return contracts;
        }

        public static List<GameObjectMeta> GetRelatedDefList(string effectName)
        {
            if (string.IsNullOrEmpty(effectName))
                return new List<GameObjectMeta>();

            IEffect effect = EffectFactory.Create(effectName);
            if (effect?.Config == null)
                return new List<GameObjectMeta>();

            return ResolveDefs(effect.Config.RelatedDefList, null);
        }

        private static List<GameObjectMeta> ResolveDefs(string categoryId, ColonySnapshot snapshot)
        {
            if (string.IsNullOrEmpty(categoryId))
                return new List<GameObjectMeta>();

            // First ones are not static, based on current snapshot
            switch (categoryId)
            {
                case DiseaseListId:
                    var diseases = SnapshotHealthList(snapshot, health => health.diseases);
                    return diseases.Count > 0 ? diseases : ItemCategoryRegistry.GetList("DiseaseList");
                case ChronicConditionListId:
                    var chronic = SnapshotHealthList(snapshot, health => health.chronic_conditions);
                    return chronic.Count > 0 ? chronic : ItemCategoryRegistry.GetList("DiseaseList");
                case MissingBodyPartListId:
                    var missing = SnapshotHealthList(snapshot, health => health.missing_body_parts);
                    return missing.Count > 0 ? missing : ItemCategoryRegistry.GetList("BodyPartList");
                case WildAnimalListId:
                    if (snapshot?.wildAnimals != null && snapshot.wildAnimals.Count > 0)
                        return snapshot.wildAnimals.Select(entry => entry.meta).ToList();
                    return ItemCategoryRegistry.GetList("AnimalList");
                case ColonyResourcesListId:
                    if (snapshot?.colonyResources != null && snapshot.colonyResources.Count > 0)
                        return snapshot.colonyResources.Select(entry => entry.meta).ToList();
                    return ItemCategoryRegistry.GetList("Resources_Metals");
                default:
                    return ItemCategoryRegistry.GetList(categoryId);
            }
        }

        private static List<GameObjectMeta> SnapshotHealthList(
            ColonySnapshot snapshot, Func<HealthSnapshot, List<GameObjectMeta>> getEntries)
        {
            List<GameObjectMeta> result = new List<GameObjectMeta>();
            if (snapshot?.colonists == null)
                return result;

            HashSet<string> seen = new HashSet<string>();
            foreach (PawnSnapshot colonist in snapshot.colonists)
            {
                foreach (GameObjectMeta entry in getEntries(colonist.health))
                {
                    if (string.IsNullOrEmpty(entry.DefName) || !seen.Add(entry.DefName))
                        continue;
                    result.Add(entry);
                }
            }

            result.Sort((a, b) => string.Compare(a.DefName, b.DefName, StringComparison.Ordinal));
            return result;
        }
    }
}