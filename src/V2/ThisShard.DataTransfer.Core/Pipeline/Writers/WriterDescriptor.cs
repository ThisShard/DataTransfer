namespace ThisShard.Database.Pipeline.Writers;

/// <summary>
/// Дескриптор для <see cref="CompositeWriter"/>
/// </summary>
public class CompositeWriterDescriptor
{
    /// <summary>
    /// Райтер
    /// </summary>
    public required IWriter Writer { get; init; }
    
    /// <summary>
    /// Минимальное количество строк для записи
    /// </summary>
    public int? MinRows { get; init; }
    
    /// <summary>
    /// Максимальное количество строк для записи
    /// </summary>
    public int? MaxRows { get; init; }
    
    /// <summary>
    /// Передать управление писателю в <see cref="CompositeWriter"/>
    /// </summary>
    public bool Owned { get; init; }
}