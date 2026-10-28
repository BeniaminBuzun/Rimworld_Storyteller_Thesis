using RimTelligence.Source.Memory.Search;
using System;
using System.Collections.Generic;

namespace RimTelligence.Source.Memory.Storage
{
    /// <summary>Save-scoped store for runtime memories (events, pawn history, ...).</summary>
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
            {
                SqlHelper.Exec(conn,
                    "INSERT INTO dynamic_chunks (category, game_tick, pawn_name, text, embedding, metadata, save_id) " +
                    "VALUES (@c,@g,@p,@t,@e,@m,@s)",
                    null,
                    ("@c", category),
                    ("@g", gameTick),
                    ("@p", pawnName),
                    ("@t", text),
                    ("@e", SqlHelper.Blob(embedding)),
                    ("@m", metadataJson),
                    ("@s", saveId));
            }
        }

        public List<RagResult> Query(float[] queryEmbedding, int topK = 5, string categoryFilter = null, string pawnFilter = null)
        {
            var sql = "SELECT embedding, text, category FROM dynamic_chunks WHERE save_id = @s";
            var parameters = new List<(string Name, object Value)> { ("@s", saveId) };

            if (categoryFilter != null)
            {
                sql += " AND category = @c";
                parameters.Add(("@c", categoryFilter));
            }
            if (pawnFilter != null)
            {
                sql += " AND pawn_name = @p";
                parameters.Add(("@p", pawnFilter));
            }

            var board = new TopKBoard(topK);
            using (var conn = OpenConnection())
            {
                ScanSimilar(conn, sql, queryEmbedding, board,
                    reader => new RagResult { Text = reader.GetString(1), Source = reader.GetString(2) },
                    parameters.ToArray());
            }
            return board.ToList();
        }

        /// <summary>Prunes old memories of this save so the table doesn't grow forever.</summary>
        public void PruneOlderThan(int minGameTick)
        {
            using (var conn = OpenConnection())
            {
                SqlHelper.Exec(conn,
                    "DELETE FROM dynamic_chunks WHERE save_id = @s AND game_tick < @t",
                    null,
                    ("@s", saveId),
                    ("@t", minGameTick));
            }
        }
    }
}
