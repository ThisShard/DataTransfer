using LargeXlsx;
using ThisShard.Database.Core.Models.Rows;
using ThisShard.Database.Core.Models.Tables;
using ThisShard.Database.Core.Writers;
using ThisShard.Database.Infrastructure.Excel.Helpers;
using ThisShard.Database.Infrastructure.Excel.Models;
using ThisShard.Database.Infrastructure.Excel.Options;

namespace ThisShard.Database.Infrastructure.Excel.Writers;

/// <summary>
/// Писатель таблицы Excel
/// </summary>
public class ExcelTableWriter : BaseTableWriter
{
    private readonly XlsxWriter _excelWriter;
    private readonly Func<IRow, bool> _rowFilter;
    private readonly string? _rowStatePropertyName;
    private readonly ExcelStyleOptions _styles;
    
    private ExcelTable _table;

    public ExcelTableWriter(XlsxWriter excelWriter, ExcelStyleOptions styles, Func<IRow, bool>? rowFilter = null, string? rowStatePropertyName = null)
    {
        _excelWriter = excelWriter ?? throw new ArgumentNullException(nameof(excelWriter));
        _styles = styles ?? throw new ArgumentNullException(nameof(styles));
        _rowFilter = rowFilter ?? (_ => true);
        _rowStatePropertyName = rowStatePropertyName;
    }

    /// <summary>
    /// Действие при инициализации таблицей
    /// </summary>
    protected override ValueTask OnInit(ITable table)
    {
        var excelTable = table as ExcelTable;

        _table = excelTable ?? throw new ArgumentOutOfRangeException(nameof(table));
        
        WriteHeader();

        return ValueTask.CompletedTask;
    }

    /// <summary>
    /// Действие при инициализации временной таблицей
    /// </summary>
    protected override ValueTask OnInit(IStagingTable table)
    {
        return OnInit(table.DestinationTable);
    }

    /// <summary>
    /// Действие при записи строк
    /// </summary>
    protected override ValueTask OnWrite(IEnumerable<IRow> rows)
    {
        foreach (var row in rows.Where(_rowFilter))
        {
            OnWrite(row);
        }
        
        return ValueTask.CompletedTask;
    }

    /// <summary>
    /// Действие при записи одиночной строки
    /// </summary>
    protected override ValueTask OnWrite(IRow row)
    {
        WriteRow(row);
        
        return ValueTask.CompletedTask;
    }

    /// <summary>
    /// Действие при принудительной записи
    /// </summary>
    protected override ValueTask OnFlush() => ValueTask.CompletedTask;

    /// <summary>
    /// Действие при завершении записи
    /// </summary>
    protected override ValueTask OnComplete() => ValueTask.CompletedTask;

    /// <summary>
    /// Действие при очистке
    /// </summary>
    protected override ValueTask OnDispose() => ValueTask.CompletedTask;

    /// <summary>
    /// Пишет заголовок
    /// </summary>
    private void WriteHeader()
    {
        _excelWriter.BeginRow();
        
        foreach (var column in _table.Columns)
        {
            _excelWriter.Write(column.Name, _styles.HeaderStyle);
        }
    }

    /// <summary>
    /// Пишет строку
    /// </summary>
    private void WriteRow(IRow row)
    {
        _excelWriter.BeginRow();

        if (_rowStatePropertyName != null)
            _excelWriter.Write(row.State.ToString("G"));
        
        foreach (var column in _table.Columns)
        {
            WriteValue(row, column);
        }
    }

    /// <summary>
    /// Пишет значение
    /// </summary>
    private void WriteValue(IRow row, ExcelColumn column)
    {
        row.TryGetValue(column.Key, out object? value);
        _excelWriter.WriteValue(value, _styles);
    }
}