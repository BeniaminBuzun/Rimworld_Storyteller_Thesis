using Microsoft.Data.Sqlite;
using System;
using System.IO;
using Verse;

namespace RimTelligence.Memory
{
    [StaticConstructorOnStartup]
    public static class RagBootstrap
    {
        public static string DbPath;

        static RagBootstrap()
        {
            SqliteBootstrap.Init(ModPaths.Root);

            string dataDir = ModPaths.Resolve("Data");
            Directory.CreateDirectory(dataDir);
            DbPath = Path.Combine(dataDir, "rag.db");

            using (var conn = new SqliteConnection("Data Source=" + DbPath))
            {
                conn.Open();
                RagMigration.EnsureSchema(conn);
                RagTableMigration.EnsureSplitTables(conn);
            }

            ModLogger.Message("SQLite RAG store ready at " + DbPath);

            try
            {
                DataSnapshots.GameObjectsCapturer.EnsurePopulatedOnStartup();
            }
            catch (Exception ex)
            {
                ModLogger.Error("RAG startup population check failed: " + ex.Message);
            }

            // Pre-warm ONNX embedding model on a background thread so the first incident does not stutter
            System.Threading.Tasks.Task.Run(() =>
            {
                try
                {
                    var _ = DataSnapshots.GameObjectsCapturer.EmbeddingProvider;
                }
                catch (Exception ex)
                {
                    ModLogger.Warning("ONNX embedding pre-warm failed: " + ex.Message);
                }
            });
        }
    }
}
