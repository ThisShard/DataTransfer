namespace ThisShard.Database.Core.Models.Rows;

/// <summary>
/// Базовая имплементация строки
/// </summary>
public class Row : IRow
{
    private IDictionary<string, object?>? _metadata;

    /// <summary>
    /// Состояние
    /// </summary>
    public RowState State { get; set; }
    
    /// <summary>
    /// Данные по ключу
    /// </summary>
    public required IReadOnlyDictionary<string, object?> Data { get; set; }

    /// <summary>
    /// Возвращает список ключей
    /// </summary>
    public IEnumerable<string> GetKeys() => Data.Keys;

    /// <summary>
    /// Метаданные строки
    /// </summary>
    public IDictionary<string, object?> Metadata => _metadata ??= new Dictionary<string, object>()!;

    /// <summary>
    /// Пытается получить значение ячейки
    /// </summary>
    public bool TryGetValue(string columnKey, out object? value) => 
        Data.TryGetValue(columnKey, out value);
}