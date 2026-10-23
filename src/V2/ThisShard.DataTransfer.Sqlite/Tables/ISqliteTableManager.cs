using Microsoft.Data.Sqlite;
using ThisShard.Database.Models;
using ThisShard.Database.Models.Tables;

namespace ThisShard.Database.Tables;

/// <summary>
/// Менеджер таблиц постгреса
/// </summary>
public interface ISqliteTableManager
{
    /// <summary>
    /// Возвращает схему таблицы для указанного пути
    /// </summary>
    Task<SqliteTable?> GetTable(SqliteConnection connection, string name);
    
    /// <summary>
    /// Создать таблицу
    /// </summary>
    Task CreateTable(SqliteConnection connection, SqliteTable table);
    
    /// <summary>
    /// Удалить таблицу
    /// </summary>
    Task DeleteTable(SqliteConnection connection, SqliteTable table);
    
    /// <summary>
    /// Конвертирует таблицу в Sqlite
    /// </summary>
    SqliteTable ConvertTable(ITable table, string? name = null);
}