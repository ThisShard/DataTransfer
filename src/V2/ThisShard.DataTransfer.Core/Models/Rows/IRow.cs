namespace ThisShard.Database.Models.Rows;

/// <summary>
/// Строка данных
/// </summary>
public interface IRow
{
    /// <summary>
    /// Состояние
    /// </summary>
    RowState State { get; }
    
    /// <summary>
    /// Возвращает список ключей
    /// </summary>
    IEnumerable<string> Keys { get; }
    
    /// <summary>
    /// Пытается получить значение по ключу
    /// </summary>
    bool TryGetValue(string key, out object? value);
    
    /// <summary>
    /// Дополнительные метаданные строки
    /// </summary>
    IDictionary<string, object?> Metadata { get; }
}