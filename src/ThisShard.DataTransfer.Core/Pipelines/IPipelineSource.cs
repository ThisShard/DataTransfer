using ThisShard.Database.Core.Readers;

namespace ThisShard.Database.Core.Pipelines;

/// <summary>
/// Источник данных
/// </summary>
public interface IPipelineSource
{
    /// <summary>
    /// Возвращает читателя
    /// </summary>
    public IRowReader GetReader();
}