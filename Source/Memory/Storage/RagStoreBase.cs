using Microsoft.Data.Sqlite;
using System;

namespace RimTelligence.Source.Memory.Storage
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
            SqliteVecBootstrap.LoadInto(conn);
            return conn;
        }
    }
}
