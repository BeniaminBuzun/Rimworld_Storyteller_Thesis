using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.Linq;

namespace RimTelligence.Memory
{
    public class StaticChunk
    {
        public string Category;
        public string DefName;
        public string Text;
        public float[] Embedding;      // null = not embedded yet
        public string MetadataJson;
    }

    public class StaticRagStore : RagStoreBase
    {
        public StaticRagStore(string dbPath) : base(dbPath) { }

        // ---------------------------------------------------------------- writes

        public void AddChunk(string category, string defName, string text, float[] embedding, string metadataJson = null)
        {
            AddChunks(new[]
            {
                new StaticChunk { Category = category, DefName = defName, Text = text, Embedding = embedding, MetadataJson = metadataJson }
            });
        }

        public void AddChunks(IEnumerable<StaticChunk> chunks)
        {
            using (var conn = OpenConnection())
            using (var tx = conn.BeginTransaction())
            {
                foreach (var chunk in chunks)
                    Insert(conn, tx, chunk);
                tx.Commit();
            }
        }

        /// <summary>
        /// Makes the DB match the scanned defs (phase 1 of population). In one transaction:
        /// removes rows that no longer exist, inserts new ones (no embedding yet), and updates changed ones.
        /// Rows whose text changed, or all rows when resetEmbeddings is true, get their embedding cleared.
        /// </summary>
        public (int Added, int Updated, int Removed) SyncDefs(IEnumerable<StaticChunk> desired, bool resetEmbeddings)
        {
            var want = new Dictionary<(string, string), StaticChunk>();
            foreach (var d in desired) want[(d.Category, d.DefName)] = d;

            int added = 0, updated = 0, removed = 0;
            var present = new HashSet<(string, string)>();

            using (var conn = OpenConnection())
            using (var tx = conn.BeginTransaction())
            {
                foreach (var table in RagTables.All)
                {
                    var rows = new List<(long Id, string Category, string DefName, string Text)>();
                    using (var cmd = conn.CreateCommand())
                    {
                        cmd.Transaction = tx;
                        cmd.CommandText = "SELECT id, category, def_name, text FROM " + table;
                        using (var r = cmd.ExecuteReader())
                            while (r.Read())
                                rows.Add((r.GetInt64(0), r.GetString(1), r.GetString(2), r.GetString(3)));
                    }

                    foreach (var row in rows)
                    {
                        var key = (row.Category, row.DefName);

                        // Keep only if wanted, in the right table, and not a duplicate.
                        bool keep = want.TryGetValue(key, out var chunk)
                                    && RagTables.For(row.Category) == table
                                    && present.Add(key);

                        if (!keep)
                        {
                            using (var cmd = conn.CreateCommand())
                            {
                                cmd.Transaction = tx;
                                cmd.CommandText = "DELETE FROM " + table + " WHERE id=@id";
                                cmd.Parameters.AddWithValue("@id", row.Id);
                                cmd.ExecuteNonQuery();
                            }
                            removed++;
                            continue;
                        }

                        bool clear = resetEmbeddings || chunk.Text != row.Text;
                        using (var cmd = conn.CreateCommand())
                        {
                            cmd.Transaction = tx;
                            cmd.CommandText = "UPDATE " + table + " SET text=@t, metadata=@m" +
                                              (clear ? ", embedding=NULL" : "") + " WHERE id=@id";
                            cmd.Parameters.AddWithValue("@t", chunk.Text);
                            cmd.Parameters.AddWithValue("@m", (object)chunk.MetadataJson ?? DBNull.Value);
                            cmd.Parameters.AddWithValue("@id", row.Id);
                            cmd.ExecuteNonQuery();
                        }
                        if (clear) updated++;
                    }
                }

                foreach (var kv in want)
                {
                    if (present.Contains(kv.Key)) continue;
                    Insert(conn, tx, kv.Value);
                    added++;
                }

                tx.Commit();
            }

            return (added, updated, removed);
        }

        /// <summary>Stores embeddings for existing rows (phase 2 of population).</summary>
        public void SetEmbeddings(IEnumerable<StaticChunk> chunks)
        {
            using (var conn = OpenConnection())
            using (var tx = conn.BeginTransaction())
            {
                foreach (var chunk in chunks)
                    using (var cmd = conn.CreateCommand())
                    {
                        cmd.Transaction = tx;
                        cmd.CommandText = "UPDATE " + RagTables.For(chunk.Category) +
                                          " SET embedding=@e WHERE category=@c AND def_name=@d";
                        cmd.Parameters.AddWithValue("@e", RagBlob.ToBlob(chunk.Embedding));
                        cmd.Parameters.AddWithValue("@c", chunk.Category);
                        cmd.Parameters.AddWithValue("@d", chunk.DefName);
                        cmd.ExecuteNonQuery();
                    }
                tx.Commit();
            }
        }

        public void ClearAll()
        {
            using (var conn = OpenConnection())
                foreach (var table in RagTables.All)
                    using (var cmd = conn.CreateCommand())
                    {
                        cmd.CommandText = "DELETE FROM " + table;
                        cmd.ExecuteNonQuery();
                    }
        }

        private static void Insert(SQLiteConnection conn, SQLiteTransaction tx, StaticChunk chunk)
        {
            using (var cmd = conn.CreateCommand())
            {
                cmd.Transaction = tx;
                cmd.CommandText = "INSERT INTO " + RagTables.For(chunk.Category) +
                    " (category, def_name, text, embedding, metadata) VALUES (@c,@d,@t,@e,@m)";
                cmd.Parameters.AddWithValue("@c", chunk.Category);
                cmd.Parameters.AddWithValue("@d", chunk.DefName);
                cmd.Parameters.AddWithValue("@t", chunk.Text);
                cmd.Parameters.AddWithValue("@e", chunk.Embedding != null ? (object)RagBlob.ToBlob(chunk.Embedding) : DBNull.Value);
                cmd.Parameters.AddWithValue("@m", (object)chunk.MetadataJson ?? DBNull.Value);
                cmd.ExecuteNonQuery();
            }
        }

        // ---------------------------------------------------------------- reads

        public int Count()
        {
            int total = 0;
            using (var conn = OpenConnection())
                foreach (var table in RagTables.All)
                    using (var cmd = conn.CreateCommand())
                    {
                        cmd.CommandText = "SELECT COUNT(*) FROM " + table;
                        total += Convert.ToInt32(cmd.ExecuteScalar());
                    }
            return total;
        }

        public bool IsEmpty()
        {
            return Count() == 0;
        }

        /// <summary>All rows without embeddings: used by the registry to rebuild its lists.</summary>
        public List<StaticChunk> ReadDefs()
        {
            var list = new List<StaticChunk>();
            using (var conn = OpenConnection())
                foreach (var table in RagTables.All)
                    using (var cmd = conn.CreateCommand())
                    {
                        cmd.CommandText = "SELECT category, def_name, text, metadata FROM " + table;
                        using (var r = cmd.ExecuteReader())
                            while (r.Read())
                                list.Add(new StaticChunk
                                {
                                    Category = r.GetString(0),
                                    DefName = r.GetString(1),
                                    Text = r.GetString(2),
                                    MetadataJson = r.IsDBNull(3) ? null : r.GetString(3)
                                });
                    }
            return list;
        }

        /// <summary>Rows still waiting for an embedding.</summary>
        public List<StaticChunk> GetPendingEmbeddings()
        {
            var list = new List<StaticChunk>();
            using (var conn = OpenConnection())
                foreach (var table in RagTables.All)
                    using (var cmd = conn.CreateCommand())
                    {
                        cmd.CommandText = "SELECT category, def_name, text FROM " + table + " WHERE embedding IS NULL";
                        using (var r = cmd.ExecuteReader())
                            while (r.Read())
                                list.Add(new StaticChunk
                                {
                                    Category = r.GetString(0),
                                    DefName = r.GetString(1),
                                    Text = r.GetString(2)
                                });
                    }
            return list;
        }

        /// <summary>
        /// Similarity search over rows that have an embedding. Searches both tables by default;
        /// pass table = RagTables.Items or RagTables.Others to scope it.
        /// </summary>
        public List<RagResult> Query(float[] queryEmbedding, int topK = 5,
                                     string categoryFilter = null, string table = null)
        {
            string[] tables = table != null ? new[] { table } : RagTables.All;
            var results = new List<RagResult>();

            using (var conn = OpenConnection())
                foreach (var t in tables)
                    using (var cmd = conn.CreateCommand())
                    {
                        cmd.CommandText = "SELECT text, category, def_name, metadata, embedding FROM " + t +
                                          " WHERE embedding IS NOT NULL" +
                                          (categoryFilter == null ? "" : " AND category = @c");
                        if (categoryFilter != null) cmd.Parameters.AddWithValue("@c", categoryFilter);

                        using (var reader = cmd.ExecuteReader())
                            while (reader.Read())
                            {
                                byte[] blob = (byte[])reader["embedding"];
                                results.Add(new RagResult
                                {
                                    Text = reader.GetString(0),
                                    Source = reader.GetString(1),
                                    DefName = reader.GetString(2),
                                    Metadata = reader.IsDBNull(3) ? null : reader.GetString(3),
                                    Score = RagBlob.CosineSim(queryEmbedding, RagBlob.FromBlob(blob))
                                });
                            }
                    }

            return results.OrderByDescending(r => r.Score).Take(topK).ToList();
        }

        // ---------------------------------------------------------------- embedding source
        // Stored in SQLite's built-in PRAGMA user_version (0 = unknown, 1 = BuiltIn, 2 = Provider),
        // so no extra table is needed.

        public string GetEmbeddingSource()
        {
            using (var conn = OpenConnection())
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = "PRAGMA user_version";
                switch (Convert.ToInt32(cmd.ExecuteScalar()))
                {
                    case 1: return "BuiltIn";
                    case 2: return "Provider";
                    default: return null;
                }
            }
        }

        public void SetEmbeddingSource(string source)
        {
            int version = source == "Provider" ? 2 : 1;
            using (var conn = OpenConnection())
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = "PRAGMA user_version = " + version;
                cmd.ExecuteNonQuery();
            }
        }
    }
}