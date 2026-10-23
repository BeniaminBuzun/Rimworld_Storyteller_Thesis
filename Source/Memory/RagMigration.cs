using System;
using System.Data.SQLite;

namespace RimTelligence.Memory
{
    public static class RagMigration
    {
        public static void EnsureSchema(SQLiteConnection conn)
        {
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

            Exec(conn, @"
                CREATE TABLE IF NOT EXISTS save_registry (
                    save_id TEXT PRIMARY KEY,
                    last_seen TEXT NOT NULL
                );");

            MigrateDynamicChunksForSaveScoping(conn);
        }

        private static void MigrateDynamicChunksForSaveScoping(SQLiteConnection conn)
        {
            try
            {
                if (!HasColumn(conn, "dynamic_chunks", "save_id"))
                {
                    Exec(conn, "ALTER TABLE dynamic_chunks ADD COLUMN save_id TEXT;");
                    Exec(conn, "DELETE FROM dynamic_chunks WHERE save_id IS NULL;");
                }

                Exec(conn, "CREATE INDEX IF NOT EXISTS idx_dynamic_save ON dynamic_chunks(save_id);");
            }
            catch (Exception ex)
            {
                ModLogger.Error("Failed to migrate dynamic_chunks for save scoping: " + ex.Message);
            }
        }

        private static bool HasColumn(SQLiteConnection conn, string tableName, string columnName)
        {
            var cmd = conn.CreateCommand();
            cmd.CommandText = "PRAGMA table_info(" + tableName + ");";
            using (var reader = cmd.ExecuteReader())
            {
                while (reader.Read())
                {
                    if (string.Equals(reader.GetString(1), columnName, StringComparison.OrdinalIgnoreCase))
                        return true;
                }
            }
            return false;
        }

        private static void Exec(SQLiteConnection conn, string sql)
        {
            var cmd = conn.CreateCommand();
            cmd.CommandText = sql;
            cmd.ExecuteNonQuery();
        }
    }
}
