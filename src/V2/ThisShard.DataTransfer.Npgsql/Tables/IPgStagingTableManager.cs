using Npgsql;
using ThisShard.Database.Models.Tables;

namespace ThisShard.Database.Tables;

/// <summary>
/// Менеджер временных таблиц
/// </summary>
public interface IPgStagingTableManager
{
    /// <summary>
    /// Создать временную таблицу
    /// </summary>
    Task<PgStagingTable> CreateStagingTable(NpgsqlConnection connection, PgTable table);
    
    /// <summary>
    /// Создать временную таблицу
    /// </summary>
    Task CreateStagingTable(NpgsqlConnection connection, PgStagingTable table);
    
    /// <summary>
    /// Удалить временную таблицу
    /// </summary>
    Task DeleteStagingTable(NpgsqlConnection connection, PgStagingTable table);
}