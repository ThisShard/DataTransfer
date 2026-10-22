using ThisShard.Database.Models.Tables;
using ThisShard.Database.Writers;

namespace ThisShard.Database.Pipelines.Destinations;

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
    public ValueTask<IWriter> GetWriter();
}