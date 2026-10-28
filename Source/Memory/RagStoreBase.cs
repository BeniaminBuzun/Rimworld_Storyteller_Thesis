using System;
using Microsoft.Data.Sqlite;

namespace RimTelligence.Memory
{
    public abstract class RagStoreBase
    {
        protected readonly string ConnStr;

        protected RagStoreBase(string dbPath)
        {
            if (string.IsNullOrEmpty(dbPath))
                throw new ArgumentException("dbPath must be non-empty.", nameof(dbPath));

            ConnStr = "Data Source=" + dbPath;
        }

        protected SqliteConnection OpenConnection()
        {
            var conn = new SqliteConnection(ConnStr);
            conn.Open();
            return conn;
        }

        /// <summary>
        /// Streams the rows of <paramref name="sql"/> and feeds the best matches into <paramref name="board"/>.
        /// Convention: column 0 must be the embedding BLOB; <paramref name="map"/> builds the result
        /// (only called for rows that make the board). Rows with a NULL embedding, or one whose
        /// dimension differs from the query (e.g. embedded with another model), are skipped.
        /// </summary>
        protected static void ScanSimilar(SqliteConnection conn, string sql, float[] queryEmbedding,
                                          TopKBoard board, Func<SqliteDataReader, RagResult> map,
                                          params (string Name, object Value)[] parameters)
        {
            ScanSimilarFiltered(conn, sql, queryEmbedding, board, map, null, parameters);
        }

        protected static void ScanSimilarFiltered(SqliteConnection conn, string sql, float[] queryEmbedding,
                                                  TopKBoard board, Func<SqliteDataReader, RagResult> map,
                                                  Func<SqliteDataReader, bool> filter,
                                                  params (string Name, object Value)[] parameters)
        {
            using (var cmd = SqlHelper.Command(conn, null, sql, parameters))
            using (var reader = cmd.ExecuteReader())
            {
                while (reader.Read())
                {
                    if (reader.IsDBNull(0)) continue;
                    if (filter != null && !filter(reader)) continue;

                    float[] vector = RagBlob.FromBlob((byte[])reader.GetValue(0));
                    float score;
                    if (!RagBlob.TryCosineSim(queryEmbedding, vector, out score)) continue;
                    if (!board.Qualifies(score)) continue;

                    RagResult result = map(reader);
                    result.Score = score;
                    board.Add(result);
                }
            }
        }
    }
}
