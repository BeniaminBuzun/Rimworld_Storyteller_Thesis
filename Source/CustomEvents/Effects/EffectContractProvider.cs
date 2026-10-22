using RimTelligence.DataSnapshots;
using RimTelligence.Source.CustomEvents.Handlers;
using System;
using System.Collections.Generic;
using System.Linq;

namespace RimTelligence.CustomEvents.Handlers
{
    public static class EffectContractProvider
    {
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
                // for effects that are item based
                if (string.IsNullOrEmpty(primaryList) && (string.Equals(config.EffectName, "ResourcePodCrash", StringComparison.OrdinalIgnoreCase) || string.Equals(config.EffectName, "PawnFoundItems", StringComparison.OrdinalIgnoreCase)))
                {
                    primaryList = ItemCategoryRegistry.DrawRandomCategory(); // draw random item category
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

        private static List<GameObjectMeta> ResolveDefs(string categoryName, ColonySnapshot snapshot)
        {
            if (string.IsNullOrEmpty(categoryName))
                return new List<GameObjectMeta>();

            // Are not static, based on current snapshot
            switch (categoryName)
            {
                case "Colonists_Diseases":
                    return SnapshotHealthList(snapshot, health => health.diseases); ;
                case "Colonists_Chronic_Conditions":
                    return SnapshotHealthList(snapshot, health => health.chronic_conditions); ;
                case "Colonists_Missing_Body_Parts":
                    return SnapshotHealthList(snapshot, health => health.missing_body_parts);
                case "Wild_Animals":
                        return snapshot.wildAnimals.Select(entry => entry.meta).ToList();
                case "Colony_Resources":
                    return snapshot.colonyResources.Select(entry => entry.meta).ToList();
                case "Colony_Charvestable_Crops":
                    return snapshot.cultivatedPlants.Select(entry => entry.meta).ToList();
                default:
                    return ItemCategoryRegistry.GetList(categoryName);
            }
        }

        // helper to extract all health based conditions on the map 
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