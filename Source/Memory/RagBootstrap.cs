using RimTelligence.Source.CustomEvents;
using System;
using System.Data.SQLite;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using Verse;

namespace RimTelligence
{
    [StaticConstructorOnStartup]
    public static class RagBootstrap
    {
        public static string DbPath;

        [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        private static extern IntPtr LoadLibrary(string lpFileName);

        static RagBootstrap()
        {
            string assembliesDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            string modRoot = Path.GetFullPath(Path.Combine(assembliesDir, ".."));

            string dllPath = Path.Combine(modRoot, "Native", "SQLite.Interop.dll");
            if (File.Exists(dllPath)) LoadLibrary(dllPath);

            string dataDir = Path.Combine(modRoot, "Data");
            Directory.CreateDirectory(dataDir);
            DbPath = Path.Combine(dataDir, "rag.db");

            using (var conn = new SQLiteConnection("Data Source=" + DbPath + ";Version=3;"))
            {
                conn.Open();
                Exec(conn, @"
                    CREATE TABLE IF NOT EXISTS static_chunks (
                        id INTEGER PRIMARY KEY AUTOINCREMENT,
                        category TEXT NOT NULL,
                        def_name TEXT NOT NULL,
                        text TEXT NOT NULL,
                        embedding BLOB NOT NULL,
                        metadata TEXT
                    );");
                Exec(conn, "CREATE INDEX IF NOT EXISTS idx_static_category ON static_chunks(category);");

                Exec(conn, @"
                    CREATE TABLE IF NOT EXISTS dynamic_chunks (
                        id INTEGER PRIMARY KEY AUTOINCREMENT,
                        category TEXT NOT NULL,
                        game_tick INTEGER NOT NULL,
                        pawn_name TEXT,
                        text TEXT NOT NULL,
                        embedding BLOB NOT NULL,
                        metadata TEXT
                    );");
                Exec(conn, "CREATE INDEX IF NOT EXISTS idx_dynamic_category ON dynamic_chunks(category);");
                Exec(conn, "CREATE INDEX IF NOT EXISTS idx_dynamic_tick ON dynamic_chunks(game_tick);");
            }

            ModLogger.Message("SQLite RAG store ready at " + DbPath);
        }

        private static void Exec(SQLiteConnection conn, string sql)
        {
            var cmd = conn.CreateCommand();
            cmd.CommandText = sql;
            cmd.ExecuteNonQuery();
        }
    }
}