namespace ThisShard.Database.Core.Models.Rows;

/// <summary>
/// Интерфейс строки
/// </summary>
public interface IRow
{
    /// <summary>
    /// Состояние
    /// </summary>
    RowState State { get; }
    
    /// <summary>
    /// Пытается получить значение ячейки
    /// </summary>
    bool TryGetValue(string columnKey, out object? value);
    
    /// <summary>
    /// Возвращает список ключей
    /// </summary>
    IEnumerable<string> GetKeys();
    
    /// <summary>
    /// Метаданные строки
    /// </summary>
    IDictionary<string, object?> Metadata { get; }
}