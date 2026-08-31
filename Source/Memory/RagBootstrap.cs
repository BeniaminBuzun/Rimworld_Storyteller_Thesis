using RimTelligence.CustomEvents;
using System.Data.SQLite;
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
            NativeDependencyLoader.EnsureNativeLoaded("SQLite.Interop.dll");

            string dataDir = ModPaths.Resolve("Data");
            Directory.CreateDirectory(dataDir);
            DbPath = Path.Combine(dataDir, "rag.db");

            using (var conn = new SQLiteConnection("Data Source=" + DbPath + ";Version=3;"))
            {
                conn.Open();
                RagMigration.EnsureSchema(conn);
            }

            ModLogger.Message("SQLite RAG store ready at " + DbPath);
        }
    }
}