using ThisShard.Database.Models.Rows;
using ThisShard.Database.Writers;

namespace ThisShard.Database.Pipelines.States;

/// <summary>
/// Состояние выполнения конвейера
/// </summary>
public record PipelineState
{
    /// <summary>
    /// Состояние записи
    /// </summary>
    public WritingState State { get; init; }
    
    /// <summary>
    /// Исключение
    /// </summary>
    public Exception? Exception { get; init; }
    
    /// <summary>
    /// Последний ключ источника из которого была произведена запись
    /// </summary>
    public string? LastWrittenSourceKey { get; init; }
    
    /// <summary>
    /// Последняя записанная строка
    /// </summary>
    public IRow? LastWrittenRow { get; init; }
}