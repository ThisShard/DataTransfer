using ThisShard.Database.Core.Models;
using ThisShard.Database.Core.Models.Columns;
using ThisShard.Database.Core.Models.Tables;

namespace ThisShard.Database.Infrastructure.Postgres.Models;

/// <summary>
/// Временная таблица Postgres
/// </summary>
public class PgStagingTable : IStagingTable
{
    /// <summary>
    /// Таблица назначения
    /// </summary>
    public required PgTable DestinationTable { get; set; }
    
    /// <summary>
    /// Ключ
    /// </summary>
    public required string Key { get; set; }

    /// <summary>
    /// Путь в БД
    /// </summary>
    public required string Path { get; set; }
    
    /// <summary>
    /// Исходный путь
    /// </summary>
    public required string[] RawPath { get; set; }
    
    /// <summary>
    /// Столбцы временной таблицы
    /// </summary>
    public required IReadOnlyList<PgStagingColumn> Columns { get; set; }

    /// <summary>
    /// Таблица назначения
    /// </summary>
    ITable IStagingTable.DestinationTable => DestinationTable;

    /// <summary>
    /// Столбцы временной таблицы
    /// </summary>
    IReadOnlyList<IStagingColumn> IStagingTable.Columns => Columns;
}