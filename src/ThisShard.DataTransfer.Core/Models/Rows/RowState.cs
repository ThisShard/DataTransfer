namespace ThisShard.Database.Core.Models.Rows;

/// <summary>
/// Состояние строки
/// </summary>
public enum RowState
{
    /// <summary>
    /// Игнорировать
    /// </summary>
    Ignored = 0,
    
    /// <summary>
    /// Добавлено (с валидацией)
    /// </summary>
    Added = 1,
    
    /// <summary>
    /// Изменено (с валидацией)
    /// </summary>
    Modified = 2,
    
    /// <summary>
    /// Удалено (с валидацией)
    /// </summary>
    Deleted = 3,
    
    /// <summary>
    /// Добавлено или изменено
    /// </summary>
    AddedOrModified = 4,
    
    /// <summary>
    /// Удалено (без валидации)
    /// </summary>
    SafeDeleted = 5
}