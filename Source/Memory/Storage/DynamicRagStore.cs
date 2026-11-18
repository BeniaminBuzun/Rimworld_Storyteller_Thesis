using RimTelligence.Source.Memory.Search;
using System;
using System.Collections.Generic;

namespace RimTelligence.Source.Memory.Storage
{
    /// <summary>
    /// Save-scoped store for runtime memories (events, pawn history, ...).
    /// Queries use sqlite-vec's vec0 virtual table for KNN search.
    /// </summary>
    public class DynamicRagStore : RagStoreBase
    {
        private readonly string saveId;

        public DynamicRagStore(string dbPath, string saveId) : base(dbPath)
        {
            if (string.IsNullOrEmpty(saveId))
                throw new ArgumentException("saveId must be non-empty for a save-scoped dynamic store.", nameof(saveId));
            this.saveId = saveId;
        }

        public void AddChunk(string category, int gameTick, string pawnName, string text, float[] embedding, string metadataJson = null)
        {
            if (embedding == null)
                throw new ArgumentNullException(nameof(embedding));

            using (var conn = OpenConnection())
            using (var tx = conn.BeginTransaction())
            {
                // Insert into relational table
                SqlHelper.Exec(conn,
                    "INSERT INTO dynamic_chunks (category, game_tick, pawn_name, text, embedding, metadata, save_id) " +
                    "VALUES (@c,@g,@p,@t,@e,@m,@s)",
                    tx,
                    ("@c", category),
                    ("@g", gameTick),
                    ("@p", pawnName),
                    ("@t", text),
                    ("@e", SqlHelper.Blob(embedding)),
                    ("@m", metadataJson),
                    ("@s", saveId));

                // Insert into vec0 shadow table
                var rowId = SqlHelper.Scalar(conn, "SELECT last_insert_rowid()", tx);
                byte[] vecBlob = RagBlob.ToBlob(embedding);
                SqlHelper.Exec(conn,
                    "INSERT INTO " + RagSchema.VecDynamicTableName + " (rowid, embedding) VALUES (@r, @e)",
                    tx,
                    ("@r", rowId),
                    ("@e", vecBlob));

                tx.Commit();
            }
        }

        public List<RagResult> Query(float[] queryEmbedding, int topK = 5, string categoryFilter = null, string pawnFilter = null)
        {
            byte[] queryBlob = RagBlob.ToBlob(queryEmbedding);

            // Over-fetch from vec0 to account for post-filtering by save_id, category, pawn
            int fetchK = topK * 20;

            string sql = @"
                WITH knn AS (
                    SELECT rowid, distance
                    FROM " + RagSchema.VecDynamicTableName + @"
                    WHERE embedding MATCH @query
                      AND k = @k
                )
                SELECT d.text, d.category, knn.distance
                FROM knn
                INNER JOIN dynamic_chunks d ON d.id = knn.rowid
                WHERE d.save_id = @s" +
                (categoryFilter != null ? " AND d.category = @c" : "") +
                (pawnFilter != null ? " AND d.pawn_name = @p" : "") +
                " ORDER BY knn.distance ASC";

            var results = new List<RagResult>();
            using (var conn = OpenConnection())
            {
                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = sql;
                    cmd.Parameters.AddWithValue("@query", queryBlob);
                    cmd.Parameters.AddWithValue("@k", fetchK);
                    cmd.Parameters.AddWithValue("@s", saveId);
                    if (categoryFilter != null)
                        cmd.Parameters.AddWithValue("@c", categoryFilter);
                    if (pawnFilter != null)
                        cmd.Parameters.AddWithValue("@p", pawnFilter);

                    using (var reader = cmd.ExecuteReader())
                    {
                        int count = 0;
                        while (reader.Read() && count < topK)
                        {
                            double distance = reader.GetDouble(2);
                            float score = 1.0f - (float)distance;

                            results.Add(new RagResult
                            {
                                Text = reader.GetString(0),
                                Source = reader.GetString(1),
                                Score = score
                            });
                            count++;
                        }
                    }
                }
            }

            return results;
        }

        /// <summary>Prunes old memories of this save so the table doesn't grow forever.</summary>
        public void PruneOlderThan(int minGameTick)
        {
            using (var conn = OpenConnection())
            using (var tx = conn.BeginTransaction())
            {
                // First collect the ids to prune so we can also remove from vec0
                var ids = SqlHelper.Read(conn,
                    "SELECT id FROM dynamic_chunks WHERE save_id = @s AND game_tick < @t",
                    r => r.GetInt64(0),
                    tx,
                    ("@s", saveId),
                    ("@t", minGameTick));

                if (ids.Count > 0)
                {
                    // Delete from vec0 shadow
                    foreach (long id in ids)
                        SqlHelper.Exec(conn,
                            "DELETE FROM " + RagSchema.VecDynamicTableName + " WHERE rowid=@id",
                            tx, ("@id", id));

                    // Delete from relational table
                    SqlHelper.Exec(conn,
                        "DELETE FROM dynamic_chunks WHERE save_id = @s AND game_tick < @t",
                        tx,
                        ("@s", saveId),
                        ("@t", minGameTick));
                }

                tx.Commit();
            }
        }
    }
}
