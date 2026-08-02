namespace ThisShard.Database.Core.Writers;

/// <summary>
/// Состояние писателя
/// </summary>
public enum WriterState
{
    /// <summary>
    /// Создан
    /// </summary>
    Created,
    
    /// <summary>
    /// Инициализирован
    /// </summary>
    Initialized,
    
    /// <summary>
    /// Производится запись
    /// </summary>
    Writing,
    
    /// <summary>
    /// Запись завершена
    /// </summary>
    Completed,
    
    /// <summary>
    /// Задиспозен
    /// </summary>
    Disposed
}