using Newtonsoft.Json;
using System;
using System.IO;
using System.Linq;
using Verse;

namespace RimTelligence.DataSnapshots
{
    public static class DataExporter
    {
        private static readonly string OutputPath = @"C:\Users\Karol\Desktop\resources.json";

        public static void ExportAllDefs()
        {
            try
            {
                var snapshot = ItemCategoryRegistry.AllCategories
                    .ToDictionary(kvp => kvp.Key, kvp => kvp.Value.Items);

                string json = JsonConvert.SerializeObject(snapshot, Formatting.Indented);
                File.WriteAllText(OutputPath, json);

                Log.Message($"[RimTelligence] Data exported to: {OutputPath}");
            }
            catch (Exception exception)
            {
                Log.Error($"[RimTelligence] Failed to export data: {exception}");
            }
        }
        public static void ExportColonySnapshot(ColonySnapshot colonySnapshot)
        {
            string OutputPath = @"C:\Users\Karol\Desktop\snapshot.json";

            try
            {
                string json = JsonConvert.SerializeObject(colonySnapshot, Formatting.Indented);
                File.WriteAllText(OutputPath, json);

                Log.Message($"[RimTelligence] Data exported to: {OutputPath}");
            }
            catch (Exception exception)
            {
                Log.Error($"[RimTelligence] Failed to export data: {exception}");
            }

        }
    }
}
