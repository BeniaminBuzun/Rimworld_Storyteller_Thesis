using RimTelligence.DataSnapshots;
using System.Data.SQLite;
using System.Linq;

namespace RimTelligence.Memory
{
    public static class RagTableMigration
    {
        /// <summary>
        /// Creates the items/others tables and migrates the legacy static_chunks table.
        /// If legacy rows use category ids that no longer exist (old naming), they are
        /// discarded instead of migrated, so the startup population rebuilds the store.
        /// </summary>
        public static void EnsureSplitTables(SQLiteConnection conn)
        {
            foreach (var t in RagTables.All)
            {
                Exec(conn, "CREATE TABLE IF NOT EXISTS " + t + @" (
                        id INTEGER PRIMARY KEY AUTOINCREMENT,
                        category TEXT NOT NULL,
                        def_name TEXT NOT NULL,
                        text TEXT NOT NULL,
                        embedding BLOB,
                        metadata TEXT)");
                Exec(conn, "CREATE INDEX IF NOT EXISTS idx_" + t + "_category ON " + t + "(category)");
            }

            if (!TableExists(conn, "static_chunks")) return;

            // Touching IsLoaded forces the registry to load item-categories.json (fills RagTables).
            // If loading failed, keep the legacy table and retry on the next start.
            if (!ItemCategoryRegistry.IsLoaded) return;

            string[] itemIds = RagTables.ItemCategoryIds;
            string[] knownIds = ItemCategoryRegistry.AllCategories.Keys.ToArray();
            if (itemIds.Length == 0 || knownIds.Length == 0) return;

            using (var tx = conn.BeginTransaction())
            {
                if (CountWhere(conn, "category NOT IN (" + Marks(knownIds) + ")", knownIds) == 0)
                {
                    Move(conn, RagTables.Items, "category IN (" + Marks(itemIds) + ")", itemIds);
                    Move(conn, RagTables.Others, "category NOT IN (" + Marks(itemIds) + ")", itemIds);
                    ModLogger.Message("Migrated static_chunks into items/others tables.");
                }
                else
                {
                    ModLogger.Message("Legacy static_chunks uses outdated category ids; discarding so the store is repopulated.");
                }

                Exec(conn, "DROP TABLE static_chunks");
                tx.Commit();
            }
        }

        private static string Marks(string[] ids) =>
            string.Join(",", ids.Select((_, i) => "@p" + i));

        private static void Move(SQLiteConnection conn, string target, string where, string[] ids)
        {
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = "INSERT INTO " + target + " (category, def_name, text, embedding, metadata) " +
                                  "SELECT category, def_name, text, embedding, metadata FROM static_chunks WHERE " + where;
                for (int i = 0; i < ids.Length; i++) cmd.Parameters.AddWithValue("@p" + i, ids[i]);
                cmd.ExecuteNonQuery();
            }
        }

        private static int CountWhere(SQLiteConnection conn, string where, string[] ids)
        {
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = "SELECT COUNT(*) FROM static_chunks WHERE " + where;
                for (int i = 0; i < ids.Length; i++) cmd.Parameters.AddWithValue("@p" + i, ids[i]);
                return System.Convert.ToInt32(cmd.ExecuteScalar());
            }
        }

        private static bool TableExists(SQLiteConnection conn, string name)
        {
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = "SELECT 1 FROM sqlite_master WHERE type='table' AND name=@n";
                cmd.Parameters.AddWithValue("@n", name);
                return cmd.ExecuteScalar() != null;
            }
        }

        private static void Exec(SQLiteConnection conn, string sql)
        {
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = sql;
                cmd.ExecuteNonQuery();
            }
        }
    }
}