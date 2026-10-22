using ThisShard.Database.Models.Tables;

namespace ThisShard.Database.Writers;

/// <summary>
/// Райтер в реляционную таблицу с использованием Bulk операций
/// </summary>
public abstract class BulkWriter : TableWriter
{
    /// <summary>
    /// Схема временной таблицы
    /// </summary>
    protected IStagingTableSchema StagingTableSchema { get; set; }
    
    /// <summary>
    /// Райтер владеет временной таблицей
    /// </summary>
    protected bool OwnsStagingTable { get; set; }
    
    protected BulkWriter(int bufferSize) : base(bufferSize)
    {
    }
}