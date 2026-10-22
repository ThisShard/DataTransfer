using ThisShard.Database.Models.Rows;
using ThisShard.Database.Models.Tables;
using ThisShard.Database.Readers;

namespace ThisShard.Database.Pipelines.Sources;

/// <summary>
/// Источник данных для конвейера
/// </summary>
public interface IPipelineSource : IAsyncDisposable
{
    /// <summary>
    /// Ключ
    /// </summary>
    public string Key { get; }

    /// <summary>
    /// Возвращает таблицу
    /// </summary>
    public ValueTask<ITable?> GetTable();
    
    /// <summary>
    /// Возвращает читателя
    /// </summary>
    public ValueTask<IReader> GetReader(IRow? lastWrittenRow = null);
}