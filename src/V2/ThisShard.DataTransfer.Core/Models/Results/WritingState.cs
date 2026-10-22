namespace ThisShard.Database.Models.Results;

/// <summary>
/// Состояние записи
/// </summary>
public enum WritingState
{
    /// <summary>
    /// Успешно
    /// </summary>
    Success,
    
    /// <summary>
    /// Отменено
    /// </summary>
    Canceled,
    
    /// <summary>
    /// Ошибка
    /// </summary>
    Error
}