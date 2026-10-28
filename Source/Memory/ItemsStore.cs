using System.Collections.Generic;

namespace RimTelligence.Memory
{
    /// <summary>
    /// Legacy items-only store. StaticRagStore covers the same table
    /// (Query(..., table: RagTables.Items)), so this can be deleted once nothing references it.
    /// </summary>
    public class ItemsStore : RagStoreBase
    {
        public ItemsStore(string dbPath) : base(dbPath) { }

        public void AddChunk(string category, string defName, string text, float[] embedding, string metadataJson = null)
        {
            using (var conn = OpenConnection())
            {
                SqlHelper.Exec(conn,
                    "INSERT INTO " + RagTables.Items + " (category, def_name, text, embedding, metadata) VALUES (@c,@d,@t,@e,@m)",
                    null,
                    ("@c", category),
                    ("@d", defName),
                    ("@t", text),
                    ("@e", SqlHelper.Blob(embedding)),
                    ("@m", metadataJson));
            }
        }

        public int Count()
        {
            using (var conn = OpenConnection())
                return SqlHelper.Count(conn, "SELECT COUNT(*) FROM " + RagTables.Items);
        }

        public bool IsEmpty()
        {
            return Count() == 0;
        }

        public List<RagResult> Query(float[] queryEmbedding, int topK = 5, string categoryFilter = null)
        {
            var sql = "SELECT embedding, text, category, def_name, metadata FROM " + RagTables.Items +
                      " WHERE embedding IS NOT NULL";
            if (categoryFilter != null) sql += " AND category = @c";

            var board = new TopKBoard(topK);
            using (var conn = OpenConnection())
            {
                if (categoryFilter != null)
                    ScanSimilar(conn, sql, queryEmbedding, board, StaticRagStore.MapResult, ("@c", categoryFilter));
                else
                    ScanSimilar(conn, sql, queryEmbedding, board, StaticRagStore.MapResult);
            }
            return board.ToList();
        }
    }
}
