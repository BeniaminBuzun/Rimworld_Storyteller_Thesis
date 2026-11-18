using Microsoft.Data.Sqlite;
using RimTelligence.Source.Memory.Search;
using RimTelligence.Source.Memory.Storage;
using System;
using System.Collections.Generic;

namespace RimTelligence.Memory
{
    /// <summary>
    /// Store for def-derived knowledge (items / others tables). Population is two-phase:
    /// SyncDefs writes text + metadata, then SetEmbeddings fills in vectors later.
    /// 
    /// Queries use sqlite-vec's vec0 virtual tables for hardware-accelerated KNN search
    /// instead of scanning every row and computing cosine similarity in C#.
    /// </summary>
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
        /// removes rows that no longer exist (or sit in the wrong table / are duplicates),
        /// inserts new ones without an embedding, and updates changed ones.
        /// Rows whose text changed, or all rows when resetEmbeddings is true, get their embedding cleared.
        /// Unchanged rows are not rewritten.
        /// </summary>
        public (int Added, int Updated, int Removed) SyncDefs(IEnumerable<StaticChunk> desired, bool resetEmbeddings)
        {
            var want = new Dictionary<(string, string), StaticChunk>();
            foreach (var d in desired) want[(d.Category, d.DefName)] = d;

            var present = new HashSet<(string, string)>();
            int added = 0, updated = 0, removed = 0;

            using (var conn = OpenConnection())
            using (var tx = conn.BeginTransaction())
            {
                foreach (var table in RagTables.All)
                {
                    var rows = SqlHelper.Read(conn,
                        "SELECT id, category, def_name, text, metadata FROM " + table,
                        r => (Id: r.GetInt64(0), Category: r.GetString(1), DefName: r.GetString(2),
                              Text: r.GetString(3), Metadata: SqlHelper.NullableString(r, 4)),
                        tx);

                    foreach (var row in rows)
                    {
                        var key = (row.Category, row.DefName);

                        // Keep only if wanted, in the right table, and not a duplicate.
                        StaticChunk chunk;
                        bool keep = want.TryGetValue(key, out chunk)
                                    && RagTables.For(row.Category) == table
                                    && present.Add(key);

                        if (!keep)
                        {
                            SqlHelper.Exec(conn, "DELETE FROM " + table + " WHERE id=@id", tx, ("@id", row.Id));
                            // Also remove from vec0 shadow table
                            SqlHelper.Exec(conn, "DELETE FROM " + RagSchema.VecTableName(table) + " WHERE rowid=@id", tx, ("@id", row.Id));
                            removed++;
                            continue;
                        }

                        bool clearEmbedding = resetEmbeddings || chunk.Text != row.Text;
                        bool changed = clearEmbedding || chunk.MetadataJson != row.Metadata;
                        if (!changed) continue;

                        SqlHelper.Exec(conn,
                            "UPDATE " + table + " SET text=@t, metadata=@m" + (clearEmbedding ? ", embedding=NULL" : "") + " WHERE id=@id",
                            tx,
                            ("@t", chunk.Text),
                            ("@m", chunk.MetadataJson),
                            ("@id", row.Id));

                        if (clearEmbedding)
                        {
                            // Remove stale embedding from vec0
                            SqlHelper.Exec(conn, "DELETE FROM " + RagSchema.VecTableName(table) + " WHERE rowid=@id", tx, ("@id", row.Id));
                            updated++;
                        }
                    }
                }

                foreach (var pair in want)
                {
                    if (present.Contains(pair.Key)) continue;
                    Insert(conn, tx, pair.Value);
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
                {
                    string table = RagTables.For(chunk.Category);

                    // Update the BLOB in the relational table
                    SqlHelper.Exec(conn,
                        "UPDATE " + table + " SET embedding=@e WHERE category=@c AND def_name=@d",
                        tx,
                        ("@e", SqlHelper.Blob(chunk.Embedding)),
                        ("@c", chunk.Category),
                        ("@d", chunk.DefName));

                    // Upsert into the vec0 shadow table.
                    // First get the row id from the parent table.
                    var rowId = SqlHelper.Scalar(conn,
                        "SELECT id FROM " + table + " WHERE category=@c AND def_name=@d",
                        tx,
                        ("@c", chunk.Category),
                        ("@d", chunk.DefName));

                    if (rowId != null && !(rowId is DBNull) && chunk.Embedding != null)
                    {
                        string vecTable = RagSchema.VecTableName(table);
                        byte[] vecBlob = RagBlob.ToBlob(chunk.Embedding);

                        // Delete old entry if exists, then insert new
                        SqlHelper.Exec(conn, "DELETE FROM " + vecTable + " WHERE rowid=@r", tx, ("@r", rowId));
                        SqlHelper.Exec(conn,
                            "INSERT INTO " + vecTable + " (rowid, embedding) VALUES (@r, @e)",
                            tx,
                            ("@r", rowId),
                            ("@e", vecBlob));
                    }
                }
                tx.Commit();
            }
        }

        public void ClearAll()
        {
            using (var conn = OpenConnection())
            {
                foreach (var table in RagTables.All)
                {
                    SqlHelper.Exec(conn, "DELETE FROM " + table);
                    SqlHelper.Exec(conn, "DELETE FROM " + RagSchema.VecTableName(table));
                }
            }
        }

        private static void Insert(SqliteConnection conn, SqliteTransaction tx, StaticChunk chunk)
        {
            string table = RagTables.For(chunk.Category);

            SqlHelper.Exec(conn,
                "INSERT INTO " + table +
                " (category, def_name, text, embedding, metadata) VALUES (@c,@d,@t,@e,@m)",
                tx,
                ("@c", chunk.Category),
                ("@d", chunk.DefName),
                ("@t", chunk.Text),
                ("@e", SqlHelper.Blob(chunk.Embedding)),   // NULL until phase 2
                ("@m", chunk.MetadataJson));

            // If embedding is provided at insert time, also insert into vec0
            if (chunk.Embedding != null)
            {
                var rowId = SqlHelper.Scalar(conn, "SELECT last_insert_rowid()", tx);
                string vecTable = RagSchema.VecTableName(table);
                byte[] vecBlob = RagBlob.ToBlob(chunk.Embedding);
                SqlHelper.Exec(conn,
                    "INSERT INTO " + vecTable + " (rowid, embedding) VALUES (@r, @e)",
                    tx,
                    ("@r", rowId),
                    ("@e", vecBlob));
            }
        }

        // ---------------------------------------------------------------- reads

        public int Count()
        {
            int total = 0;
            using (var conn = OpenConnection())
                foreach (var table in RagTables.All)
                    total += SqlHelper.Count(conn, "SELECT COUNT(*) FROM " + table);
            return total;
        }

        public bool IsEmpty()
        {
            return Count() == 0;
        }

        /// <summary>All rows without embeddings: used by the registry to rebuild its lists.</summary>
        public List<StaticChunk> ReadDefs()
        {
            return ReadAcrossTables("SELECT category, def_name, text, metadata FROM {0}",
                r => new StaticChunk
                {
                    Category = r.GetString(0),
                    DefName = r.GetString(1),
                    Text = r.GetString(2),
                    MetadataJson = SqlHelper.NullableString(r, 3)
                });
        }

        /// <summary>Rows still waiting for an embedding.</summary>
        public List<StaticChunk> GetPendingEmbeddings()
        {
            return ReadAcrossTables("SELECT category, def_name, text FROM {0} WHERE embedding IS NULL",
                r => new StaticChunk
                {
                    Category = r.GetString(0),
                    DefName = r.GetString(1),
                    Text = r.GetString(2)
                });
        }

        /// <summary>
        /// KNN search using sqlite-vec's vec0 virtual tables. Searches both tables by default;
        /// pass table = RagTables.Items or RagTables.Others to scope it.
        /// When defNames is provided, only rows whose def_name is contained in the collection are scored.
        /// 
        /// Unlike the old scan-based approach, this uses vec0's MATCH operator for
        /// hardware-accelerated approximate nearest neighbor search.
        /// </summary>
        public List<RagResult> Query(float[] queryEmbedding, int topK = 5,
                                     string categoryFilter = null, string table = null,
                                     ICollection<string> defNames = null)
        {
            string[] tables = ResolveTables(table);
            var results = new List<RagResult>();

            byte[] queryBlob = RagBlob.ToBlob(queryEmbedding);

            HashSet<string> defNameSet = defNames != null && defNames.Count > 0
                ? new HashSet<string>(defNames, StringComparer.Ordinal)
                : null;

            using (var conn = OpenConnection())
            {
                foreach (var t in tables)
                {
                    string vecTable = RagSchema.VecTableName(t);

                    // We ask vec0 for more results than needed, then post-filter by category/defName.
                    // This is necessary because vec0 metadata columns don't cover our relational filters.
                    int fetchK = topK;
                    if (categoryFilter != null || defNameSet != null)
                        fetchK = topK * 10; // over-fetch to account for filtering

                    // KNN via vec0 MATCH, then JOIN back to the parent table for text + metadata
                    string sql = @"
                        WITH knn AS (
                            SELECT rowid, distance
                            FROM " + vecTable + @"
                            WHERE embedding MATCH @query
                              AND k = @k
                        )
                        SELECT t.text, t.category, t.def_name, t.metadata, knn.distance
                        FROM knn
                        INNER JOIN " + t + @" t ON t.id = knn.rowid
                        WHERE 1=1" +
                        (categoryFilter != null ? " AND t.category = @cat" : "") +
                        " ORDER BY knn.distance ASC";

                    using (var cmd = conn.CreateCommand())
                    {
                        cmd.CommandText = sql;
                        cmd.Parameters.AddWithValue("@query", queryBlob);
                        cmd.Parameters.AddWithValue("@k", fetchK);
                        if (categoryFilter != null)
                            cmd.Parameters.AddWithValue("@cat", categoryFilter);

                        using (var reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                string defName = reader.GetString(2);
                                if (defNameSet != null && !defNameSet.Contains(defName))
                                    continue;

                                // cosine distance -> cosine similarity: sim = 1 - dist
                                double distance = reader.GetDouble(4);
                                float score = 1.0f - (float)distance;

                                results.Add(new RagResult
                                {
                                    Text = reader.GetString(0),
                                    Source = reader.GetString(1),
                                    DefName = defName,
                                    Metadata = SqlHelper.NullableString(reader, 3),
                                    Score = score
                                });
                            }
                        }
                    }
                }
            }

            // Merge results from multiple tables, sort by score descending, take topK
            results.Sort((a, b) => b.Score.CompareTo(a.Score));
            if (results.Count > topK)
                results.RemoveRange(topK, results.Count - topK);

            return results;
        }

        private static string[] ResolveTables(string table)
        {
            if (table == null) return RagTables.All;
            if (Array.IndexOf(RagTables.All, table) < 0)
                throw new ArgumentException("Unknown RAG table: " + table, nameof(table));
            return new[] { table };
        }

        private List<T> ReadAcrossTables<T>(string sqlFormat, Func<SqliteDataReader, T> map)
        {
            var list = new List<T>();
            using (var conn = OpenConnection())
                foreach (var table in RagTables.All)
                    list.AddRange(SqlHelper.Read(conn, string.Format(sqlFormat, table), map));
            return list;
        }

        // ---------------------------------------------------------------- embedding source
        // Stored in SQLite's built-in PRAGMA user_version (0 = unknown, 1 = BuiltIn, 2 = Provider),
        // so no extra table is needed.

        private const int SourceBuiltIn = 1;
        private const int SourceProvider = 2;

        public string GetEmbeddingSource()
        {
            using (var conn = OpenConnection())
            {
                switch (SqlHelper.Count(conn, "PRAGMA user_version"))
                {
                    case SourceBuiltIn: return "BuiltIn";
                    case SourceProvider: return "Provider";
                    default: return null;
                }
            }
        }

        public void SetEmbeddingSource(string source)
        {
            int version = source == "Provider" ? SourceProvider : SourceBuiltIn;
            using (var conn = OpenConnection())
                SqlHelper.Exec(conn, "PRAGMA user_version = " + version);
        }
    }
}
