using System;
using System.Collections.Generic;
using System.Linq;

namespace RimTelligence.Memory
{
    public class ItemsStore : RagStoreBase
    {
        public ItemsStore(string dbPath) : base(dbPath) { }

        public void AddChunk(string category, string defName, string text, float[] embedding, string metadataJson = null)
        {
            using (var conn = OpenConnection())
            {
                var cmd = conn.CreateCommand();
                cmd.CommandText = "INSERT INTO items (category, def_name, text, embedding, metadata) VALUES (@c,@d,@t,@e,@m)";
                cmd.Parameters.AddWithValue("@c", category);
                cmd.Parameters.AddWithValue("@d", defName);
                cmd.Parameters.AddWithValue("@t", text);
                cmd.Parameters.AddWithValue("@e", RagBlob.ToBlob(embedding));
                cmd.Parameters.AddWithValue("@m", (object)metadataJson ?? DBNull.Value);
                cmd.ExecuteNonQuery();
            }
        }

        public int Count()
        {
            using (var conn = OpenConnection())
            {
                var cmd = conn.CreateCommand();
                cmd.CommandText = "SELECT COUNT(*) FROM items";
                return System.Convert.ToInt32(cmd.ExecuteScalar());
            }
        }

        public bool IsEmpty()
        {
            return Count() == 0;
        }

        public List<RagResult> Query(float[] queryEmbedding, int topK = 5, string categoryFilter = null)
        {
            var results = new List<RagResult>();
            using (var conn = OpenConnection())
            {
                var cmd = conn.CreateCommand();
                cmd.CommandText = categoryFilter == null
                        ? "SELECT text, category, def_name, metadata, embedding FROM items"
                        : "SELECT text, category, def_name, metadata, embedding FROM items WHERE category = @c";
                if (categoryFilter != null) cmd.Parameters.AddWithValue("@c", categoryFilter);

                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        byte[] blob = (byte[])reader["embedding"];
                        float score = RagBlob.CosineSim(queryEmbedding, RagBlob.FromBlob(blob));
                        results.Add(new RagResult
                        {
                            Text = reader.GetString(0),
                            Source = reader.GetString(1),
                            DefName = reader.GetString(2),
                            Metadata = reader.IsDBNull(3) ? null : reader.GetString(3),
                            Score = score
                        });
                    }
                }
            }
            return results.OrderByDescending(result => result.Score).Take(topK).ToList();
        }
    }
}