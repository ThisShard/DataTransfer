namespace ThisShard.Database.Pipeline.Writers;

/// <summary>
/// Статус райтера
/// </summary>
public enum WriterStatus
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