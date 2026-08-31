using System;
using System.Collections.Generic;
using System.Linq;

namespace RimTelligence.Memory
{
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
            using (var conn = OpenConnection())
            {
                var cmd = conn.CreateCommand();
                cmd.CommandText = "INSERT INTO dynamic_chunks (category, game_tick, pawn_name, text, embedding, metadata, save_id) VALUES (@c,@g,@p,@t,@e,@m,@s)";
                cmd.Parameters.AddWithValue("@c", category);
                cmd.Parameters.AddWithValue("@g", gameTick);
                cmd.Parameters.AddWithValue("@p", (object)pawnName ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@t", text);
                cmd.Parameters.AddWithValue("@e", RagBlob.ToBlob(embedding));
                cmd.Parameters.AddWithValue("@m", (object)metadataJson ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@s", saveId);
                cmd.ExecuteNonQuery();
            }
        }

        public List<RagResult> Query(float[] queryEmbedding, int topK = 5, string categoryFilter = null, string pawnFilter = null)
        {
            var results = new List<RagResult>();
            using (var conn = OpenConnection())
            {
                var cmd = conn.CreateCommand();
                var where = new List<string> { "save_id = @s" };
                if (categoryFilter != null) where.Add("category = @c");
                if (pawnFilter != null) where.Add("pawn_name = @p");
                string whereClause = " WHERE " + string.Join(" AND ", where);
                cmd.CommandText = "SELECT text, category, embedding FROM dynamic_chunks" + whereClause;
                cmd.Parameters.AddWithValue("@s", saveId);
                if (categoryFilter != null) cmd.Parameters.AddWithValue("@c", categoryFilter);
                if (pawnFilter != null) cmd.Parameters.AddWithValue("@p", pawnFilter);

                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        byte[] blob = (byte[])reader["embedding"];
                        float score = RagBlob.CosineSim(queryEmbedding, RagBlob.FromBlob(blob));
                        results.Add(new RagResult { Text = reader.GetString(0), Source = reader.GetString(1), Score = score });
                    }
                }
            }
            return results.OrderByDescending(result => result.Score).Take(topK).ToList();
        }

        // Optional: prune old memories so the table doesn't grow forever
        public void PruneOlderThan(int minGameTick)
        {
            using (var conn = OpenConnection())
            {
                var cmd = conn.CreateCommand();
                cmd.CommandText = "DELETE FROM dynamic_chunks WHERE save_id = @s AND game_tick < @t";
                cmd.Parameters.AddWithValue("@s", saveId);
                cmd.Parameters.AddWithValue("@t", minGameTick);
                cmd.ExecuteNonQuery();
            }
        }
    }
}