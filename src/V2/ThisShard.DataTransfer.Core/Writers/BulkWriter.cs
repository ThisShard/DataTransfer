using ThisShard.Database.Models.Tables;

namespace ThisShard.Database.Writers;

/// <summary>
/// Райтер в реляционную таблицу с использованием Bulk операций
/// </summary>
public abstract class BulkWriter : RelationalWriter
{
    /// <summary>
    /// Схема временной таблицы
    /// </summary>
    protected IStagingTableSchema StagingTableSchema { get; init; }
    
    /// <summary>
    /// Райтер владеет временной таблицей
    /// </summary>
    protected bool OwnsStagingTable { get; init; }
    
    protected BulkWriter(IStagingTableSchema stagingTableSchema, ITableSchema tableSchema, bool ownsStagingTable, int bufferSize) : base(tableSchema, bufferSize)
    {
        OwnsStagingTable = ownsStagingTable;
    }
}