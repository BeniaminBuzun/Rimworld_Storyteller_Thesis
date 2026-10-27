using Microsoft.Data.Sqlite;
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
            catch (System.Exception ex)
            {
                ModLogger.Error("RAG startup population check failed: " + ex.Message);
            }
        }
    }
}