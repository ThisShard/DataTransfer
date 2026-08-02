namespace ThisShard.Database.Core.Models.Rows;

/// <summary>
/// Строка с измененным стейтом
/// </summary>
public class StateRowWrapper : IRow
{
    private IRow _rowImplementation;

    public StateRowWrapper(IRow row)
    {
        _rowImplementation = row;
        State = row.State;
    }

    /// <summary>
    /// Состояние
    /// </summary>
    public RowState State { get; set; }

    /// <summary>
    /// Пытается получить значение ячейки
    /// </summary>
    public bool TryGetValue(string columnKey, out object? value) => 
        _rowImplementation.TryGetValue(columnKey, out value);
}