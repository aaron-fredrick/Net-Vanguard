using System;
using Microsoft.Data.Sqlite;
using NetVanguard.Core.Models;
using System.Collections.Concurrent;

namespace NetVanguard.Core.Services
{
    public class SqliteStatisticsService : IStatisticsService, IDisposable
    {
        private readonly string _connectionString;
        private readonly ConcurrentDictionary<string, (long TotalSent, long TotalReceived, long MaxSent, long MaxReceived, long TotalBlocked)> _processCache = new();
        private readonly ConcurrentDictionary<string, (long TotalSent, long TotalReceived, long MaxSent, long MaxReceived, long TotalBlocked)> _domainCache = new();

        public SqliteStatisticsService(string? dbPath = null)
        {
            dbPath ??= Path.Combine(AppContext.BaseDirectory, "netvanguard_stats.db");
            _connectionString = $"Data Source={dbPath}";
            InitializeDatabase();
            LoadCache();
        }

        private void InitializeDatabase()
        {
            using var connection = new SqliteConnection(_connectionString);
            connection.Open();

            var command = connection.CreateCommand();
            command.CommandText = @"
                CREATE TABLE IF NOT EXISTS ProcessStats (
                    ProcessName TEXT PRIMARY KEY,
                    TotalSent INTEGER,
                    TotalReceived INTEGER,
                    MaxSent INTEGER,
                    MaxReceived INTEGER,
                    TotalBlocked INTEGER DEFAULT 0
                );
                CREATE TABLE IF NOT EXISTS DomainStats (
                    RemoteAddress TEXT PRIMARY KEY,
                    TotalSent INTEGER,
                    TotalReceived INTEGER,
                    MaxSent INTEGER,
                    MaxReceived INTEGER,
                    TotalBlocked INTEGER DEFAULT 0
                );";
            command.ExecuteNonQuery();
        }

        private void LoadCache()
        {
            using var connection = new SqliteConnection(_connectionString);
            connection.Open();

            var procCmd = connection.CreateCommand();
            procCmd.CommandText = "SELECT ProcessName, TotalSent, TotalReceived, MaxSent, MaxReceived, TotalBlocked FROM ProcessStats";
            using (var reader = procCmd.ExecuteReader())
            {
                while (reader.Read())
                {
                    _processCache[reader.GetString(0)] = (reader.GetInt64(1), reader.GetInt64(2), reader.GetInt64(3), reader.GetInt64(4), reader.FieldCount > 5 ? reader.GetInt64(5) : 0);
                }
            }

            var domCmd = connection.CreateCommand();
            domCmd.CommandText = "SELECT RemoteAddress, TotalSent, TotalReceived, MaxSent, MaxReceived, TotalBlocked FROM DomainStats";
            using (var reader = domCmd.ExecuteReader())
            {
                while (reader.Read())
                {
                    _domainCache[reader.GetString(0)] = (reader.GetInt64(1), reader.GetInt64(2), reader.GetInt64(3), reader.GetInt64(4), reader.FieldCount > 5 ? reader.GetInt64(5) : 0);
                }
            }
        }

        public void UpdateProcessStats(string processName, long bytesSent, long bytesReceived, long maxBpsSent, long maxBpsReceived, long blockedBytes)
        {
            _processCache.AddOrUpdate(processName, 
                (bytesSent, bytesReceived, maxBpsSent, maxBpsReceived, blockedBytes),
                (_, old) => (old.TotalSent + bytesSent, old.TotalReceived + bytesReceived, Math.Max(old.MaxSent, maxBpsSent), Math.Max(old.MaxReceived, maxBpsReceived), old.TotalBlocked + blockedBytes));
        }

        public void UpdateDomainStats(string remoteAddress, long bytesSent, long bytesReceived, long maxBpsSent, long maxBpsReceived, long blockedBytes)
        {
            _domainCache.AddOrUpdate(remoteAddress, 
                (bytesSent, bytesReceived, maxBpsSent, maxBpsReceived, blockedBytes),
                (_, old) => (old.TotalSent + bytesSent, old.TotalReceived + bytesReceived, Math.Max(old.MaxSent, maxBpsSent), Math.Max(old.MaxReceived, maxBpsReceived), old.TotalBlocked + blockedBytes));
        }

        public (long TotalSent, long TotalReceived, long MaxSent, long MaxReceived, long TotalBlocked) GetProcessLifetimeStats(string processName)
        {
            return _processCache.TryGetValue(processName, out var stats) ? stats : (0, 0, 0, 0, 0);
        }

        public (long TotalSent, long TotalReceived, long MaxSent, long MaxReceived, long TotalBlocked) GetDomainLifetimeStats(string remoteAddress)
        {
            return _domainCache.TryGetValue(remoteAddress, out var stats) ? stats : (0, 0, 0, 0, 0);
        }

        public void Save()
        {
            using var connection = new SqliteConnection(_connectionString);
            connection.Open();
            using var transaction = connection.BeginTransaction();

            try
            {
                foreach (var kvp in _processCache)
                {
                    var cmd = connection.CreateCommand();
                    cmd.Transaction = transaction;
                    cmd.CommandText = @"
                        INSERT INTO ProcessStats (ProcessName, TotalSent, TotalReceived, MaxSent, MaxReceived, TotalBlocked)
                        VALUES ($name, $ts, $tr, $ms, $mr, $tb)
                        ON CONFLICT(ProcessName) DO UPDATE SET
                            TotalSent = $ts,
                            TotalReceived = $tr,
                            MaxSent = $ms,
                            MaxReceived = $mr,
                            TotalBlocked = $tb;";
                    cmd.Parameters.AddWithValue("$name", kvp.Key);
                    cmd.Parameters.AddWithValue("$ts", kvp.Value.TotalSent);
                    cmd.Parameters.AddWithValue("$tr", kvp.Value.TotalReceived);
                    cmd.Parameters.AddWithValue("$ms", kvp.Value.MaxSent);
                    cmd.Parameters.AddWithValue("$mr", kvp.Value.MaxReceived);
                    cmd.Parameters.AddWithValue("$tb", kvp.Value.TotalBlocked);
                    cmd.ExecuteNonQuery();
                }

                foreach (var kvp in _domainCache)
                {
                    var cmd = connection.CreateCommand();
                    cmd.Transaction = transaction;
                    cmd.CommandText = @"
                        INSERT INTO DomainStats (RemoteAddress, TotalSent, TotalReceived, MaxSent, MaxReceived, TotalBlocked)
                        VALUES ($addr, $ts, $tr, $ms, $mr, $tb)
                        ON CONFLICT(RemoteAddress) DO UPDATE SET
                            TotalSent = $ts,
                            TotalReceived = $tr,
                            MaxSent = $ms,
                            MaxReceived = $mr,
                            TotalBlocked = $tb;";
                    cmd.Parameters.AddWithValue("$addr", kvp.Key);
                    cmd.Parameters.AddWithValue("$ts", kvp.Value.TotalSent);
                    cmd.Parameters.AddWithValue("$tr", kvp.Value.TotalReceived);
                    cmd.Parameters.AddWithValue("$ms", kvp.Value.MaxSent);
                    cmd.Parameters.AddWithValue("$mr", kvp.Value.MaxReceived);
                    cmd.Parameters.AddWithValue("$tb", kvp.Value.TotalBlocked);
                    cmd.ExecuteNonQuery();
                }

                transaction.Commit();
            }
            catch
            {
                transaction.Rollback();
                throw;
            }
        }

        public void Dispose()
        {
            Save();
        }
    }
}
