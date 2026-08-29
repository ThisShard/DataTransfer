using ThisShard.Database.Core.Models.Tables;
using ThisShard.Database.Core.Writers;

namespace ThisShard.Database.Core.Pipelines.Destinations;

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
    /// Инициализирует таблицей
    /// </summary>
    public ValueTask Init(ITable table);
    
    /// <summary>
    /// Возвращает писателя
    /// </summary>
    public ValueTask<IRowWriter> GetWriter();
}