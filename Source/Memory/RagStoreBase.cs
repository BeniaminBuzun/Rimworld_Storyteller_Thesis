using System;
using System.Data.SQLite;

namespace RimTelligence.Memory
{
    public abstract class RagStoreBase
    {
        protected readonly string ConnStr;

        protected RagStoreBase(string dbPath)
        {
            if (string.IsNullOrEmpty(dbPath))
                throw new ArgumentException("dbPath must be non-empty.", nameof(dbPath));

            ConnStr = "Data Source=" + dbPath + ";Version=3;";
        }

        protected SQLiteConnection OpenConnection()
        {
            var conn = new SQLiteConnection(ConnStr);
            conn.Open();
            return conn;
        }
    }
}
