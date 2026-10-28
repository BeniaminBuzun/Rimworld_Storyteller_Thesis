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
                        if (clearEmbedding) updated++;
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
                    SqlHelper.Exec(conn,
                        "UPDATE " + RagTables.For(chunk.Category) + " SET embedding=@e WHERE category=@c AND def_name=@d",
                        tx,
                        ("@e", SqlHelper.Blob(chunk.Embedding)),
                        ("@c", chunk.Category),
                        ("@d", chunk.DefName));
                }
                tx.Commit();
            }
        }

        public void ClearAll()
        {
            using (var conn = OpenConnection())
                foreach (var table in RagTables.All)
                    SqlHelper.Exec(conn, "DELETE FROM " + table);
        }

        private static void Insert(SqliteConnection conn, SqliteTransaction tx, StaticChunk chunk)
        {
            SqlHelper.Exec(conn,
                "INSERT INTO " + RagTables.For(chunk.Category) +
                " (category, def_name, text, embedding, metadata) VALUES (@c,@d,@t,@e,@m)",
                tx,
                ("@c", chunk.Category),
                ("@d", chunk.DefName),
                ("@t", chunk.Text),
                ("@e", SqlHelper.Blob(chunk.Embedding)),   // NULL until phase 2
                ("@m", chunk.MetadataJson));
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
        /// Similarity search over rows that have an embedding. Searches both tables by default;
        /// pass table = RagTables.Items or RagTables.Others to scope it.
        /// When defNames is provided, only rows whose def_name is contained in the collection are scored.
        /// </summary>
        public List<RagResult> Query(float[] queryEmbedding, int topK = 5,
                                     string categoryFilter = null, string table = null,
                                     ICollection<string> defNames = null)
        {
            string[] tables = ResolveTables(table);
            var board = new TopKBoard(topK);

            HashSet<string> defNameSet = defNames != null && defNames.Count > 0
                ? new HashSet<string>(defNames, StringComparer.Ordinal)
                : null;

            Func<SqliteDataReader, bool> filter = defNameSet != null
                ? r => !r.IsDBNull(3) && defNameSet.Contains(r.GetString(3))
                : (Func<SqliteDataReader, bool>)null;

            using (var conn = OpenConnection())
                foreach (var t in tables)
                {
                    string sql = "SELECT embedding, text, category, def_name, metadata FROM " + t +
                                 " WHERE embedding IS NOT NULL" +
                                 (categoryFilter == null ? "" : " AND category = @c");

                    if (categoryFilter == null)
                        ScanSimilarFiltered(conn, sql, queryEmbedding, board, MapResult, filter);
                    else
                        ScanSimilarFiltered(conn, sql, queryEmbedding, board, MapResult, filter, ("@c", categoryFilter));
                }

            return board.ToList();
        }

        /// <summary>Maps (embedding, text, category, def_name, metadata) rows to results.</summary>
        internal static RagResult MapResult(SqliteDataReader r)
        {
            return new RagResult
            {
                Text = r.GetString(1),
                Source = r.GetString(2),
                DefName = r.GetString(3),
                Metadata = SqlHelper.NullableString(r, 4)
            };
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
