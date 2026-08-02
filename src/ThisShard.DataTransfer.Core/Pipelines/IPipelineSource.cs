using ThisShard.Database.Core.Readers;

namespace ThisShard.Database.Core.Pipelines;

/// <summary>
/// Источник данных для конвейера
/// </summary>
public interface IPipelineSource : IAsyncDisposable
{
    /// <summary>
    /// Возвращает читателя
    /// </summary>
    public ValueTask<IRowReader> GetReader();
}