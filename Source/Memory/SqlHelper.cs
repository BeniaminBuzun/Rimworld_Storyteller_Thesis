using System;
using System.Collections.Generic;
using Microsoft.Data.Sqlite;

namespace RimTelligence.Memory
{
    /// <summary>
    /// Small ADO.NET helpers so stores and migrations don't repeat command boilerplate.
    /// Every command created here is disposed, and carries the transaction when one is given
    /// (some Microsoft.Data.Sqlite versions refuse to run commands without it).
    /// </summary>
    public static class SqlHelper
    {
        public static object Blob(float[] embedding)
        {
            return embedding == null ? (object)DBNull.Value : RagBlob.ToBlob(embedding);
        }

        public static string NullableString(SqliteDataReader reader, int ordinal)
        {
            return reader.IsDBNull(ordinal) ? null : reader.GetString(ordinal);
        }

        public static SqliteCommand Command(SqliteConnection conn, SqliteTransaction tx, string sql,
                                            (string Name, object Value)[] parameters)
        {
            var cmd = conn.CreateCommand();
            cmd.CommandText = sql;
            if (tx != null) cmd.Transaction = tx;
            if (parameters != null)
                foreach (var p in parameters)
                    cmd.Parameters.AddWithValue(p.Name, p.Value ?? DBNull.Value);
            return cmd;
        }

        public static int Exec(SqliteConnection conn, string sql, SqliteTransaction tx = null,
                               params (string Name, object Value)[] parameters)
        {
            using (var cmd = Command(conn, tx, sql, parameters))
                return cmd.ExecuteNonQuery();
        }

        public static object Scalar(SqliteConnection conn, string sql, SqliteTransaction tx = null,
                                    params (string Name, object Value)[] parameters)
        {
            using (var cmd = Command(conn, tx, sql, parameters))
                return cmd.ExecuteScalar();
        }

        public static int Count(SqliteConnection conn, string sql, SqliteTransaction tx = null,
                                params (string Name, object Value)[] parameters)
        {
            return Convert.ToInt32(Scalar(conn, sql, tx, parameters));
        }

        public static List<T> Read<T>(SqliteConnection conn, string sql, Func<SqliteDataReader, T> map,
                                      SqliteTransaction tx = null,
                                      params (string Name, object Value)[] parameters)
        {
            var list = new List<T>();
            using (var cmd = Command(conn, tx, sql, parameters))
            using (var reader = cmd.ExecuteReader())
                while (reader.Read())
                    list.Add(map(reader));
            return list;
        }
    }
}
