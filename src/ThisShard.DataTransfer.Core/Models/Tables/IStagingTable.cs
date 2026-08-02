using ThisShard.Database.Core.Models.Columns;

namespace ThisShard.Database.Core.Models.Tables;

/// <summary>
/// Временная таблица
/// </summary>
public interface IStagingTable
{
    /// <summary>
    /// Таблица назначения
    /// </summary>
    ITable DestinationTable { get; }
    
    /// <summary>
    /// Ключ
    /// </summary>
    string Key { get; }
    
    /// <summary>
    /// Путь в БД
    /// </summary>
    string Path { get; }
    
    /// <summary>
    /// Столбцы временной таблицы
    /// </summary>
    IReadOnlyList<IStagingColumn> Columns { get; }
}