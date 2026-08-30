using ThisShard.Database.Core.Models.Rows;
using ThisShard.Database.Core.Readers;
using ThisShard.Database.Core.Writers;

namespace ThisShard.Database.Core.Models.Results;

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
    public IRowReader? Reader { get; init; }
    
    /// <summary>
    /// Писатели
    /// </summary>
    public IReadOnlyCollection<IRowWriter>? Writers { get; init; }
}