using System.Data.SQLite;
using FileNexus.Models;

namespace FileNexus.Services;

/// <summary>
/// 数据库服务 —— 使用SQLite嵌入式数据库管理文件元数据与同步历史
/// 核心功能：DDL建表、参数化DML增删改查、事务管理
/// </summary>
public class DatabaseService : IDisposable
{
    private readonly string _connectionString;

    public DatabaseService(string dbPath)
    {
        _connectionString = $"Data Source={dbPath};Version=3;";
        InitializeDatabase();
    }

    /// <summary>
    /// 初始化数据库表结构（DDL），使用IF NOT EXISTS保证幂等
    /// </summary>
    private void InitializeDatabase()
    {
        using var connection = new SQLiteConnection(_connectionString);
        connection.Open();
        string initSql = "CREATE TABLE IF NOT EXISTS FileInfo (Id INTEGER PRIMARY KEY AUTOINCREMENT, FileName TEXT NOT NULL, FullPath TEXT NOT NULL UNIQUE, FileSize INTEGER NOT NULL, FileType TEXT, Extension TEXT, CreationTime TEXT, LastModified TEXT, IndexedTime TEXT DEFAULT (datetime('now','localtime')));"
            + "CREATE TABLE IF NOT EXISTS SyncHistory (Id INTEGER PRIMARY KEY AUTOINCREMENT, SourcePath TEXT NOT NULL, DestPath TEXT NOT NULL, TotalFiles INTEGER, CopiedFiles INTEGER, SkippedFiles INTEGER, SyncTime TEXT DEFAULT (datetime('now','localtime')));"
            + "CREATE INDEX IF NOT EXISTS idx_file_extension ON FileInfo(Extension);"
            + "CREATE INDEX IF NOT EXISTS idx_file_type ON FileInfo(FileType);";
        using var cmd = new SQLiteCommand(initSql, connection);
        cmd.ExecuteNonQuery();
    }

    /// <summary>
    /// 批量插入文件信息，使用事务保证原子性 —— INSERT OR REPLACE处理重复
    /// 参数化查询防止SQL注入
    /// </summary>
    public void BatchInsertFiles(List<MediaFileInfo> files)
    {
        using var connection = new SQLiteConnection(_connectionString);
        connection.Open();
        using var transaction = connection.BeginTransaction();
        try
        {
            const string sql = @"INSERT OR REPLACE INTO FileInfo
                (FileName, FullPath, FileSize, FileType, Extension, CreationTime, LastModified)
                VALUES (@FileName, @FullPath, @FileSize, @FileType, @Extension, @CreationTime, @LastModified)";

            foreach (var file in files)
            {
                using var cmd = new SQLiteCommand(sql, connection, transaction);
                cmd.Parameters.AddWithValue("@FileName", file.FileName);
                cmd.Parameters.AddWithValue("@FullPath", file.FullPath);
                cmd.Parameters.AddWithValue("@FileSize", file.FileSize);
                cmd.Parameters.AddWithValue("@FileType", file.FileType);
                cmd.Parameters.AddWithValue("@Extension", file.Extension);
                cmd.Parameters.AddWithValue("@CreationTime", file.CreationTime.ToString("o"));
                cmd.Parameters.AddWithValue("@LastModified", file.LastModified.ToString("o"));
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

    /// <summary>
    /// 按关键词和扩展名搜索文件，使用LIKE参数化查询
    /// </summary>
    public List<MediaFileInfo> SearchFiles(string keyword, string extension = "")
    {
        var results = new List<MediaFileInfo>();
        using var connection = new SQLiteConnection(_connectionString);
        connection.Open();

        string sql = "SELECT FileName, FullPath, FileSize, FileType, Extension, CreationTime, LastModified FROM FileInfo WHERE FileName LIKE @Keyword";
        if (!string.IsNullOrEmpty(extension))
            sql += " AND Extension = @Extension";

        using var cmd = new SQLiteCommand(sql, connection);
        cmd.Parameters.AddWithValue("@Keyword", $"%{keyword}%");
        if (!string.IsNullOrEmpty(extension))
            cmd.Parameters.AddWithValue("@Extension", extension);

        using var reader = cmd.ExecuteReader();
        while (reader.Read())
        {
            results.Add(new MediaFileInfo
            {
                FileName = reader.GetString(0),
                FullPath = reader.GetString(1),
                FileSize = reader.GetInt64(2),
                FileType = reader.IsDBNull(3) ? "" : reader.GetString(3),
                Extension = reader.IsDBNull(4) ? "" : reader.GetString(4),
                CreationTime = DateTime.TryParse(reader.GetString(5), out var ct) ? ct : DateTime.MinValue,
                LastModified = DateTime.TryParse(reader.GetString(6), out var lm) ? lm : DateTime.MinValue
            });
        }
        return results;
    }

    /// <summary>
    /// 按扩展名统计文件类型分布，用于图表展示
    /// </summary>
    public Dictionary<string, int> GetFileTypeStatistics()
    {
        var stats = new Dictionary<string, int>();
        using var connection = new SQLiteConnection(_connectionString);
        connection.Open();
        using var cmd = new SQLiteCommand(
            "SELECT Extension, COUNT(*) as Cnt FROM FileInfo GROUP BY Extension ORDER BY Cnt DESC", connection);
        using var reader = cmd.ExecuteReader();
        while (reader.Read())
            stats[reader.GetString(0)] = reader.GetInt32(1);
        return stats;
    }

    /// <summary>
    /// 记录同步历史
    /// </summary>
    public void RecordSyncHistory(string source, string dest, SyncJob job)
    {
        using var connection = new SQLiteConnection(_connectionString);
        connection.Open();
        using var cmd = new SQLiteCommand(
            "INSERT INTO SyncHistory (SourcePath, DestPath, TotalFiles, CopiedFiles, SkippedFiles) VALUES (@s, @d, @t, @c, @sk)",
            connection);
        cmd.Parameters.AddWithValue("@s", source);
        cmd.Parameters.AddWithValue("@d", dest);
        cmd.Parameters.AddWithValue("@t", job.TotalFiles);
        cmd.Parameters.AddWithValue("@c", job.CopiedFiles);
        cmd.Parameters.AddWithValue("@sk", job.SkippedFiles);
        cmd.ExecuteNonQuery();
    }

    public void Dispose()
    {
        // SQLiteConnection 由 using 管理，此处保留扩展点
    }
}
