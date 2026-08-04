using ThisShard.Database.Core.Models.Rows;
using ThisShard.Database.Core.Readers;

namespace ThisShard.Database.Core.Pipelines;

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
    /// Возвращает читателя
    /// </summary>
    public ValueTask<IRowReader> GetReader(IRow? lastWrittenRow = null);
}