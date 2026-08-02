using System.Text.Json;
using ThisShard.Database.Core.Models;
using ThisShard.Database.Core.Models.Rows;
using ThisShard.Database.Core.Models.Tables;
using ThisShard.Database.Core.Writers;
using ThisShard.Database.Infrastructure.Json.Helpers;
using ThisShard.Database.Infrastructure.Json.Models;

namespace ThisShard.Database.Infrastructure.Json.Writers;

/// <summary>
/// Писатель таблицы Json
/// </summary>
public class JsonTableWriter : BaseTableWriter
{
    private readonly Utf8JsonWriter _jsonWriter;
    private readonly Func<IRow, bool> _rowFilter;
    private readonly string? _rowStatePropertyName;
    
    private JsonTable _table;

    public JsonTableWriter(Utf8JsonWriter jsonWriter, Func<IRow, bool>? rowFilter = null, string? rowStatePropertyName = null)
    {
        _jsonWriter = jsonWriter ?? throw new ArgumentNullException(nameof(jsonWriter));
        _rowFilter = rowFilter ?? (_ => true);
        _rowStatePropertyName = rowStatePropertyName;
    }

    /// <summary>
    /// Действие при инициализации таблицей
    /// </summary>
    protected override ValueTask OnInit(ITable table)
    {
        var jsonTable = table as JsonTable;

        _table = jsonTable ?? throw new ArgumentOutOfRangeException(nameof(table));
        
        WriteStartArray();

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
    protected override ValueTask OnComplete()
    {
        WriteEndArray();
        return ValueTask.CompletedTask;
    }

    /// <summary>
    /// Действие при очистке
    /// </summary>
    protected override ValueTask OnDispose() => ValueTask.CompletedTask;

    /// <summary>
    /// Пишет начало массива
    /// </summary>
    private void WriteStartArray()
    {
        _jsonWriter.WriteStartArray();
    }

    /// <summary>
    /// Пишет конец массива
    /// </summary>
    private void WriteEndArray()
    {
        _jsonWriter.WriteEndArray();
    }

    /// <summary>
    /// Пишет строку
    /// </summary>
    private void WriteRow(IRow row)
    {
        _jsonWriter.WriteStartObject();

        if (_rowStatePropertyName != null)
            _jsonWriter.WriteValue(_rowStatePropertyName, row.State.ToString("G"));
        
        foreach (var column in _table.Columns)
        {
            WriteValue(row, column);
        }
        
        _jsonWriter.WriteEndObject();
    }

    /// <summary>
    /// Пишет значение
    /// </summary>
    private void WriteValue(IRow row, JsonColumn column)
    {
        row.TryGetValue(column.Key, out object? value);
        _jsonWriter.WriteValue(column.Name, value);
    }
}