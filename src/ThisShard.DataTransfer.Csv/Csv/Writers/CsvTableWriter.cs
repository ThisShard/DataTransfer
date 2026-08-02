using CsvHelper;
using ThisShard.Database.Core.Models.Rows;
using ThisShard.Database.Core.Models.Tables;
using ThisShard.Database.Core.Writers;
using ThisShard.Database.Infrastructure.Csv.Models;

namespace ThisShard.Database.Infrastructure.Csv.Writers;

/// <summary>
/// Писатель таблицы Csv
/// </summary>
public class CsvTableWriter : BaseTableWriter
{
    private readonly CsvWriter _writer;
    private readonly Func<IRow, bool> _rowFilter;
    private readonly string? _rowStateColumnName;
    
    private CsvTable _table;

    public CsvTableWriter(CsvWriter writer, Func<IRow, bool>? rowFilter = null, string? rowStateColumnName = null)
    {
        _writer = writer ?? throw new ArgumentNullException(nameof(writer));
        _rowFilter = rowFilter ?? (_ => true);
        _rowStateColumnName = rowStateColumnName;
    }

    /// <summary>
    /// Действие при инициализации таблицей
    /// </summary>
    protected override ValueTask OnInit(ITable table)
    {
        var csvTable = table as CsvTable;

        _table = csvTable ?? throw new ArgumentOutOfRangeException(nameof(table));
        
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
        if (_rowStateColumnName != null)
            _writer.WriteField(_rowStateColumnName);
        
        foreach (var column in _table.Columns)
        {
            _writer.WriteField(column.Name);
        }

        _writer.NextRecord();
    }

    /// <summary>
    /// Пишет строку
    /// </summary>
    private void WriteRow(IRow row)
    {
        if (_rowStateColumnName != null)
            _writer.WriteField(row.State.ToString("G"));
        
        foreach (var column in _table.Columns)
        {
            row.TryGetValue(column.Key, out object? value);
            
            var stringValue = value?.ToString();
            if (stringValue == "null")
                _writer.WriteField(stringValue, true);
            else if (stringValue == null)
                _writer.WriteField("null", false);
            else
                _writer.WriteField(stringValue);
        }

        _writer.NextRecord();
    }
}