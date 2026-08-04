using ThisShard.Database.Core.Models.Rows;
using ThisShard.Database.Core.Pipelines;

namespace ThisShard.Database.Core.Models.Results;

/// <summary>
/// Результат выполнения конвейера
/// </summary>
public record PipelineState
{
    /// <summary>
    /// Состояние записи
    /// </summary>
    public WritingState State { get; init; }
    
    /// <summary>
    /// Последний источник из которого была произведена запись
    /// </summary>
    public IPipelineSource? LastWrittenSource { get; init; }
    
    /// <summary>
    /// Последняя записанная строка
    /// </summary>
    public IRow? LastWrittenRow { get; init; }
}