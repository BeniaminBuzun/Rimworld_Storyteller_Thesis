using Microsoft.Data.Sqlite;
using RimTelligence.DataSnapshots;
using System.Linq;

namespace RimTelligence.Memory
{
    public static class RagTableMigration
    {
        private const string LegacyTable = "static_chunks";

        /// <summary>
        /// Creates the items/others tables and migrates the legacy static_chunks table.
        /// If legacy rows use category ids that no longer exist (old naming), they are
        /// discarded instead of migrated, so the startup population rebuilds the store.
        /// </summary>
        public static void EnsureSplitTables(SqliteConnection conn)
        {
            foreach (var table in RagTables.All)
            {
                SqlHelper.Exec(conn, "CREATE TABLE IF NOT EXISTS " + table + @" (
                        id INTEGER PRIMARY KEY AUTOINCREMENT,
                        category TEXT NOT NULL,
                        def_name TEXT NOT NULL,
                        text TEXT NOT NULL,
                        embedding BLOB,
                        metadata TEXT)");
                SqlHelper.Exec(conn, "CREATE INDEX IF NOT EXISTS idx_" + table + "_category ON " + table + "(category)");
            }

            if (!TableExists(conn, LegacyTable)) return;

            // Touching IsLoaded forces the registry to load item-categories.json (fills RagTables).
            // If loading failed, keep the legacy table and retry on the next start.
            if (!ItemCategoryRegistry.IsLoaded) return;

            string[] itemIds = RagTables.ItemCategoryIds;
            string[] knownIds = ItemCategoryRegistry.AllCategories.Keys.ToArray();
            if (itemIds.Length == 0 || knownIds.Length == 0) return;

            using (var tx = conn.BeginTransaction())
            {
                int unknownRows = CountLegacy(conn, tx, "category NOT IN (" + Placeholders(knownIds) + ")", knownIds);
                if (unknownRows == 0)
                {
                    string inItems = Placeholders(itemIds);
                    MoveLegacy(conn, tx, RagTables.Items, "category IN (" + inItems + ")", itemIds);
                    MoveLegacy(conn, tx, RagTables.Others, "category NOT IN (" + inItems + ")", itemIds);
                    ModLogger.Message("Migrated static_chunks into items/others tables.");
                }
                else
                {
                    ModLogger.Message("Legacy static_chunks uses outdated category ids; discarding so the store is repopulated.");
                }

                SqlHelper.Exec(conn, "DROP TABLE " + LegacyTable, tx);
                tx.Commit();
            }
        }

        private static string Placeholders(string[] ids)
        {
            return string.Join(",", ids.Select((_, i) => "@p" + i));
        }

        private static (string Name, object Value)[] Params(string[] ids)
        {
            return ids.Select((id, i) => ("@p" + i, (object)id)).ToArray();
        }

        private static void MoveLegacy(SqliteConnection conn, SqliteTransaction tx, string target, string where, string[] ids)
        {
            SqlHelper.Exec(conn,
                "INSERT INTO " + target + " (category, def_name, text, embedding, metadata) " +
                "SELECT category, def_name, text, embedding, metadata FROM " + LegacyTable + " WHERE " + where,
                tx, Params(ids));
        }

        private static int CountLegacy(SqliteConnection conn, SqliteTransaction tx, string where, string[] ids)
        {
            return SqlHelper.Count(conn, "SELECT COUNT(*) FROM " + LegacyTable + " WHERE " + where, tx, Params(ids));
        }

        private static bool TableExists(SqliteConnection conn, string name)
        {
            return SqlHelper.Scalar(conn, "SELECT 1 FROM sqlite_master WHERE type='table' AND name=@n", null, ("@n", name)) != null;
        }
    }
}
