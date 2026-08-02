namespace ThisShard.Database.Core.Models.Rows;

/// <summary>
/// Базовая имплементация строки
/// </summary>
public class Row : IRow
{
    /// <summary>
    /// Состояние
    /// </summary>
    public RowState State { get; set; }
    
    /// <summary>
    /// Данные по ключу
    /// </summary>
    public required IReadOnlyDictionary<string, object?> Data { get; set; }

    /// <summary>
    /// Пытается получить значение ячейки
    /// </summary>
    public bool TryGetValue(string columnKey, out object? value) => 
        Data.TryGetValue(columnKey, out value);
}