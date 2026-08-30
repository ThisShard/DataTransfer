using ThisShard.Database.Core.Models;
using ThisShard.Database.Core.Models.Tables;

namespace ThisShard.Database.Core.Writers;

/// <summary>
/// Писатель данных в таблцу
/// </summary>
public interface ITableWriter : IRowWriter
{
    /// <summary>
    /// Текущая таблица
    /// </summary>
    ITable Table { get; }
    
    /// <summary>
    /// Текущая временная таблица
    /// </summary>
    IStagingTable? StagingTable { get; }
    
    /// <summary>
    /// Инициализация таблицей
    /// </summary>
    ValueTask Init(ITable table);
    
    /// <summary>
    /// Инициализация временной таблицей
    /// </summary>
    ValueTask Init(IStagingTable stagingTable);
}