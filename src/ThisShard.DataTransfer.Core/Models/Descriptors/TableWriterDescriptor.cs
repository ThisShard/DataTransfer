using ThisShard.Database.Core.Writers;

namespace ThisShard.Database.Core.Models.Descriptors;

/// <summary>
/// Дескриптор для CompositeTableWriter
/// </summary>
public class TableWriterDescriptor
{
    /// <summary>
    /// Писатель
    /// </summary>
    public required ITableWriter Writer { get; init; }
    
    /// <summary>
    /// Минимальное количество строк для записи
    /// </summary>
    public int? MinRows { get; init; }
    
    /// <summary>
    /// Максимальное количество строк для записи
    /// </summary>
    public int? MaxRows { get; init; }
    
    /// <summary>
    /// Передать управление писателю в CompositeTableWriter
    /// </summary>
    public bool Owned { get; init; }
}