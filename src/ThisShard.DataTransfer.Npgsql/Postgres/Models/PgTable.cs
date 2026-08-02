using ThisShard.Database.Core.Models;
using ThisShard.Database.Core.Models.Columns;
using ThisShard.Database.Core.Models.Tables;

namespace ThisShard.Database.Infrastructure.Postgres.Models;

/// <summary>
/// Таблица Postgres
/// </summary>
public class PgTable : ITable
{
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
    /// Столбцы
    /// </summary>
    public required IReadOnlyList<PgColumn> Columns { get; set; }
    
    /// <summary>
    /// Столбцы
    /// </summary>
    IReadOnlyList<IColumn> ITable.Columns => Columns;
}