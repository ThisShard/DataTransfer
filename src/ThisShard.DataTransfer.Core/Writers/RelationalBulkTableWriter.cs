using ThisShard.Database.Core.Schemas;

namespace ThisShard.Database.Core.Writers;

public abstract class RelationalBulkTableWriter : RelationalTableWriter
{
    /// <summary>
    /// Схема временной таблицы
    /// </summary>
    protected IStagingTableSchema StagingTableSchema { get; set; } = null!;

    // ReSharper disable once ConvertToPrimaryConstructor
    protected RelationalBulkTableWriter(int bufferSize) : base(bufferSize)
    {
    }
}