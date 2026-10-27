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
    }
}
