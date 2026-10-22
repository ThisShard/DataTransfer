using ThisShard.Database.Models.Rows;
using ThisShard.Database.Readers;

namespace ThisShard.Database.Writers.States;

/// <summary>
/// Результат записи
/// </summary>
public record WritingResult
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
    /// Последняя записанная строка
    /// </summary>
    public IRow? LastWrittenRow { get; init; }
    
    /// <summary>
    /// Ридер
    /// </summary>
    public IReader? Reader { get; init; }
    
    /// <summary>
    /// Писатели
    /// </summary>
    public IReadOnlyCollection<IWriter>? Writers { get; init; }
}