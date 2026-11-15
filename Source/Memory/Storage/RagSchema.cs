using Microsoft.Data.Sqlite;

namespace RimTelligence.Source.Memory.Storage
{
    public static class RagSchema
    {
        public const int DefaultEmbeddingDim = 384;

        public static void Ensure(SqliteConnection conn)
        {
            SqlHelper.Exec(conn, @"
                CREATE TABLE IF NOT EXISTS dynamic_chunks (
                    id INTEGER PRIMARY KEY AUTOINCREMENT,
                    category TEXT NOT NULL,
                    game_tick INTEGER NOT NULL,
                    pawn_name TEXT,
                    text TEXT NOT NULL,
                    embedding BLOB NOT NULL,
                    metadata TEXT,
                    save_id TEXT NOT NULL
                );");
            SqlHelper.Exec(conn, "CREATE INDEX IF NOT EXISTS idx_dynamic_save ON dynamic_chunks(save_id);");

            SqlHelper.Exec(conn, @"
                CREATE TABLE IF NOT EXISTS save_registry (
                    save_id TEXT PRIMARY KEY,
                    last_seen TEXT NOT NULL
                );");

            foreach (var table in RagTables.All)
            {
                SqlHelper.Exec(conn, "CREATE TABLE IF NOT EXISTS " + table + @" (
                        id INTEGER PRIMARY KEY AUTOINCREMENT,
                        category TEXT NOT NULL,
                        def_name TEXT NOT NULL,
                        text TEXT NOT NULL,
                        embedding BLOB,
                        metadata TEXT);");
                SqlHelper.Exec(conn, "CREATE INDEX IF NOT EXISTS idx_" + table + "_category ON " + table + "(category);");
            }

            EnsureVecTables(conn, DefaultEmbeddingDim);
        }

        public static void EnsureVecTables(SqliteConnection conn, int dim)
        {
            foreach (var table in RagTables.All)
            {
                SqlHelper.Exec(conn,
                    "CREATE VIRTUAL TABLE IF NOT EXISTS " + VecTableName(table) + " USING vec0(" +
                    "  rowid INTEGER PRIMARY KEY," +
                    "  embedding float[" + dim + "] distance_metric=cosine" +
                    ");");
            }

            SqlHelper.Exec(conn,
                "CREATE VIRTUAL TABLE IF NOT EXISTS " + VecDynamicTableName + " USING vec0(" +
                "  rowid INTEGER PRIMARY KEY," +
                "  embedding float[" + dim + "] distance_metric=cosine" +
                ");");
        }

        public static string VecTableName(string table) => "vec_" + table;

        public const string VecDynamicTableName = "vec_dynamic_chunks";
    }
}
