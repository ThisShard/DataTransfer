using System.Data;
using ThisShard.Database.Core.Extensions;

namespace ThisShard.Database.Core.Models.Rows;

public class DataRowAdapter : IRow
{
    /// <summary>
    /// Состояние
    /// </summary>
    public RowState State { get; set; }

    /// <summary>
    /// Исходная строка
    /// </summary>
    public DataRow Row { get; }

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