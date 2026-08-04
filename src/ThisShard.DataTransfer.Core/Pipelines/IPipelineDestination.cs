using ThisShard.Database.Core.Writers;

namespace ThisShard.Database.Core.Pipelines;

/// <summary>
/// Назначение данных для конвейера
/// </summary>
public interface IPipelineDestination : IAsyncDisposable
{
    /// <summary>
    /// Ключ
    /// </summary>
    public string Key { get; }
    
    /// <summary>
    /// Возвращает писателя
    /// </summary>
    public ValueTask<IRowWriter> GetWriter();
}