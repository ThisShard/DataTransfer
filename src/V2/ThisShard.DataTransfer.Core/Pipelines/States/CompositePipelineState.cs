using ThisShard.Database.Writers;
using ThisShard.Database.Writers.States;

namespace ThisShard.Database.Pipelines.States;

/// <summary>
/// Состояние выполнения составного конвейера
/// </summary>
public record CompositePipelineState
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
    /// Состояния пайплайнов
    /// </summary>
    public required IReadOnlyDictionary<string, PipelineState?> PipelineStates { get; init; }
}