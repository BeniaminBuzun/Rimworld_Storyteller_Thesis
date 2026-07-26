using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RimTelligence.Source.Memory
{
    public class StaticRagStore
    {
        private readonly string connStr;
        public StaticRagStore(string dbPath) { connStr = "Data Source=" + dbPath + ";Version=3;"; }

        public void AddChunk(string category, string defName, string text, float[] embedding, string metadataJson = null)
        {
            using (var conn = new SQLiteConnection(connStr))
            {
                conn.Open();
                var cmd = conn.CreateCommand();
                cmd.CommandText = "INSERT INTO static_chunks (category, def_name, text, embedding, metadata) VALUES (@c,@d,@t,@e,@m)";
                cmd.Parameters.AddWithValue("@c", category);
                cmd.Parameters.AddWithValue("@d", defName);
                cmd.Parameters.AddWithValue("@t", text);
                cmd.Parameters.AddWithValue("@e", RagBlob.ToBlob(embedding));
                cmd.Parameters.AddWithValue("@m", (object)metadataJson ?? DBNull.Value);
                cmd.ExecuteNonQuery();
            }
        }

        public void ClearAll()
        {
            using (var conn = new SQLiteConnection(connStr))
            {
                conn.Open();
                var cmd = conn.CreateCommand();
                cmd.CommandText = "DELETE FROM static_chunks";
                cmd.ExecuteNonQuery();
            }
        }

        public List<RagResult> Query(float[] queryEmbedding, int topK = 5, string categoryFilter = null)
        {
            var results = new List<RagResult>();
            using (var conn = new SQLiteConnection(connStr))
            {
                conn.Open();
                var cmd = conn.CreateCommand();
                cmd.CommandText = categoryFilter == null
                        ? "SELECT text, category, def_name, metadata, embedding FROM static_chunks"
                        : "SELECT text, category, def_name, metadata, embedding FROM static_chunks WHERE category = @c";
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
            return results.OrderByDescending(r => r.Score).Take(topK).ToList();
        }
    }
}
