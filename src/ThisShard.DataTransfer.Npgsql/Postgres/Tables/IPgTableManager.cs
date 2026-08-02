using Npgsql;
using ThisShard.Database.Infrastructure.Postgres.Models;

namespace ThisShard.Database.Infrastructure.Postgres.Tables;

/// <summary>
/// Менеджер таблиц постгреса
/// </summary>
public interface IPgTableManager
{
    /// <summary>
    /// Возвращает схему таблицы для указанного пути
    /// </summary>
    Task<PgTable?> GetTable(NpgsqlConnection connection, params string[] path);
    
    /// <summary>
    /// Создать таблицу
    /// </summary>
    Task CreateTable(NpgsqlConnection connection, PgTable table);
    
    /// <summary>
    /// Удалить таблицу
    /// </summary>
    Task DeleteTable(NpgsqlConnection connection, PgTable table);
}