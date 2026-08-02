namespace ThisShard.Database.Core.Models.Rows;

/// <summary>
/// Строка с измененным стейтом
/// </summary>
public class StateRowWrapper : IRow
{
    private readonly IRow _row;

    /// <summary>
    /// Состояние
    /// </summary>
    public RowState State { get; set; }

    /// <summary>
    /// Метаданные строки
    /// </summary>
    public IDictionary<string, object?> Metadata => _row.Metadata;

    public StateRowWrapper(IRow row)
    {
        _row = row;
        State = row.State;
    }
    
    /// <summary>
    /// Пытается получить значение ячейки
    /// </summary>
    public bool TryGetValue(string columnKey, out object? value) => 
        _row.TryGetValue(columnKey, out value);
}