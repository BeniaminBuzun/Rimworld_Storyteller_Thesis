using RimTelligence.DataSnapshots;
using RimTelligence.Source.CustomEvents.Handlers;
using System;
using System.Collections.Generic;

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
                primary = ResolveDefs(config.RelatedDefList, snapshot);
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

            switch (categoryId)
            {
                case DiseaseListId:
                    return SnapshotHealthList(snapshot, health => health.diseases);
                case ChronicConditionListId:
                    return SnapshotHealthList(snapshot, health => health.chronic_conditions);
                case MissingBodyPartListId:
                    return SnapshotHealthList(snapshot, health => health.missing_body_parts);
                case WildAnimalListId:
                    return SnapshotWildAnimalList(snapshot);
                case ColonyResourcesListId:
                    return SnapshotResourceList(snapshot);
                default:
                    return ItemCategoryRegistry.GetList(categoryId);
            }
        }

        // later fix so this is already in snapshoot as dict <GameObjectMeta, int>
        private static List<GameObjectMeta> SnapshotWildAnimalList(ColonySnapshot snapshot)
        {
            List<GameObjectMeta> result = new List<GameObjectMeta>();
            if (snapshot?.wildAnimals == null || snapshot.wildAnimals.Count == 0)
                return result;

            foreach (AnimalSnapshot animal in snapshot.wildAnimals.Keys)
            {
                result.Add(new GameObjectMeta { DefName = animal.name, Label = animal.name, Description = animal.description });
            }

            result.Sort((a, b) => string.Compare(a.DefName, b.DefName, StringComparison.Ordinal));
            return result;
        }

        private static List<GameObjectMeta> SnapshotHealthList(
            ColonySnapshot snapshot,
            Func<HealthSnapshot, List<string>> selector)
        {
            List<GameObjectMeta> result = new List<GameObjectMeta>();
            if (snapshot?.colonists == null)
                return result;

            HashSet<string> seen = new HashSet<string>();
            foreach (PawnSnapshot colonist in snapshot.colonists)
            {
                List<string> entries = colonist?.health != null ? selector(colonist.health) : null;
                if (entries == null)
                    continue;

                foreach (string entry in entries)
                {
                    if (string.IsNullOrEmpty(entry) || !seen.Add(entry))
                        continue;
                    result.Add(new GameObjectMeta { DefName = entry, Label = entry, Description = string.Empty });
                }
            }

            result.Sort((a, b) => string.Compare(a.DefName, b.DefName, StringComparison.Ordinal));
            return result;
        }

        private static List<GameObjectMeta> SnapshotResourceList(ColonySnapshot snapshot)
        {
            List<GameObjectMeta> result = new List<GameObjectMeta>();
            if (snapshot?.colonyResources?.allResources == null || snapshot.colonyResources.allResources.Count == 0)
                return result;

            foreach (string resourceName in snapshot.colonyResources.allResources.Keys)
            {
                if (string.IsNullOrEmpty(resourceName))
                    continue;
                result.Add(new GameObjectMeta { DefName = resourceName, Label = resourceName, Description = string.Empty });
            }

            result.Sort((a, b) => string.Compare(a.DefName, b.DefName, StringComparison.Ordinal));
            return result;
        }
    }
}