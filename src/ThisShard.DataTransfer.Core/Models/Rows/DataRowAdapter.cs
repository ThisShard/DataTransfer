using System.Data;
using ThisShard.Database.Core.Extensions;

namespace ThisShard.Database.Core.Models.Rows;

/// <summary>
/// Адаптер строки для DataRow
/// </summary>
public class DataRowAdapter : IRow
{
    private IDictionary<string, object?>? _metadata;
    
    /// <summary>
    /// Состояние
    /// </summary>
    public RowState State { get; set; }

    /// <summary>
    /// Исходная строка
    /// </summary>
    public DataRow Row { get; }

    /// <summary>
    /// Возвращает список ключей
    /// </summary>
    public IEnumerable<string> GetKeys() => Row.Table.Columns.OfType<DataColumn>().Select(c => c.ColumnName);

    /// <summary>
    /// Метаданные строки
    /// </summary>
    public IDictionary<string, object?> Metadata => _metadata ??= new Dictionary<string, object>()!;

    public DataRowAdapter(DataRow row)
    {
        Row = row;
        State = row.RowState.ToRowState();
    }

    /// <summary>
    /// Пытается получить значение ячейки
    /// </summary>
    public bool TryGetValue(string columnKey, out object? value)
    {
        value = null;
        if (!Row.Table.Columns.Contains(columnKey))
            return false;
        
        value = Row[columnKey];
        if (value == DBNull.Value)
            value = null;
        
        return true;
    }
}