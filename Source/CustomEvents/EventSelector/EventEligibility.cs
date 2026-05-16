using RimTelligence.Source.DataSnapshots;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RimTelligence.Source.CustomEvents.EventSelector
{
    public static class EventEligibility
    {
        public static bool IsEligible(string type, ColonySnapshot snapshot)
        {
            //  animal events 
            if (type == "AnimalInsanity")
                return snapshot.wildAnimals.Any();

            if (type == "SelfTame")
                return snapshot.wildAnimals.Any();

            //  environment
            if (type == "CropBlight")
                return snapshot.cultivatedPlants.Any();

            //  pawn-targeted
            if (type == "MiracleHealChronicCondition")
                return snapshot.colonists.Any(p =>
                    p.health?.chronic_conditions?.Count > 0);

            if (type == "MiracleHealMissingPart")
                return snapshot.colonists.Any(p =>
                    p.health?.missing_body_parts?.Count > 0);

            if (type == "MiracleHealDisease")
                return snapshot.colonists.Any(p =>
                    p.health?.diseases?.Count > 0);

            //  lose items — only if colony actually has some 
            if (type.StartsWith("LoseItems_"))
            {
                string category = type.Replace("LoseItems_", "");
                return HasStockpileItems(category, snapshot);
            }

            return true;
        }

        private static bool HasStockpileItems(string category, ColonySnapshot snapshot)
        {
            var allowed = Handlers.Implementations.ItemsRelated
                .EventItemMapping.GetAllowedDefs("LoseItems_" + category);

            if (allowed == null || allowed.Count == 0)
                return true;


            foreach (string defName in allowed)
            {
                if (snapshot.resources.allResources.TryGetValue(defName, out int count))
                    return true;
            }
            return false;
        }
    }
}
