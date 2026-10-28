using System;
using Microsoft.Data.Sqlite;

namespace RimTelligence.Memory
{
    public static class RagMigration
    {
        public static void EnsureSchema(SqliteConnection conn)
        {
            // save_id is part of the base schema; databases created before save scoping
            // get it added by MigrateDynamicChunksForSaveScoping below.
            SqlHelper.Exec(conn, @"
                CREATE TABLE IF NOT EXISTS dynamic_chunks (
                    id INTEGER PRIMARY KEY AUTOINCREMENT,
                    category TEXT NOT NULL,
                    game_tick INTEGER NOT NULL,
                    pawn_name TEXT,
                    text TEXT NOT NULL,
                    embedding BLOB NOT NULL,
                    metadata TEXT,
                    save_id TEXT
                );");
            SqlHelper.Exec(conn, "CREATE INDEX IF NOT EXISTS idx_dynamic_category ON dynamic_chunks(category);");
            SqlHelper.Exec(conn, "CREATE INDEX IF NOT EXISTS idx_dynamic_tick ON dynamic_chunks(game_tick);");

            SqlHelper.Exec(conn, @"
                CREATE TABLE IF NOT EXISTS save_registry (
                    save_id TEXT PRIMARY KEY,
                    last_seen TEXT NOT NULL
                );");

            MigrateDynamicChunksForSaveScoping(conn);
        }

        private static void MigrateDynamicChunksForSaveScoping(SqliteConnection conn)
        {
            try
            {
                if (!HasColumn(conn, "dynamic_chunks", "save_id"))
                {
                    SqlHelper.Exec(conn, "ALTER TABLE dynamic_chunks ADD COLUMN save_id TEXT;");
                    // Pre-scoping rows can't be attributed to any save.
                    SqlHelper.Exec(conn, "DELETE FROM dynamic_chunks WHERE save_id IS NULL;");
                }

                SqlHelper.Exec(conn, "CREATE INDEX IF NOT EXISTS idx_dynamic_save ON dynamic_chunks(save_id);");
            }
            catch (Exception ex)
            {
                ModLogger.Error("Failed to migrate dynamic_chunks for save scoping: " + ex.Message);
            }
        }

        private static bool HasColumn(SqliteConnection conn, string tableName, string columnName)
        {
            var columns = SqlHelper.Read(conn, "PRAGMA table_info(" + tableName + ");", r => r.GetString(1));
            return columns.Exists(name => string.Equals(name, columnName, StringComparison.OrdinalIgnoreCase));
        }
    }
}
