using System.Data;
using System.Data.Common;
using ThisShard.Database.Core.Converters;
using ThisShard.Database.Core.Mappers;
using ThisShard.Database.Core.Models;
using ThisShard.Database.Core.Models.Results;
using ThisShard.Database.Core.Models.Rows;
using ThisShard.Database.Core.Readers;
using ThisShard.Database.Core.Writers;

namespace ThisShard.Database.Core.Extensions;

public static class RowWriterExtensions
{
    /// <summary>
    /// Производит запись DataTable
    /// </summary>
    public static ValueTask Write(this IRowWriter writer, DataTable table, RowState? state = null) => 
        writer.Write(table.Rows.OfType<DataRow>(), state);

    /// <summary>
    /// Производит запись DataRow
    /// </summary>
    public static async ValueTask Write(this IRowWriter writer, IEnumerable<DataRow> rows, RowState? state = null)
    {
        if (state == null)
        {
            await writer.Write(rows.Select(r => new DataRowAdapter(r)));
            return;
        }
        
        await writer.Write(rows.Select(r => new DataRowAdapter(r)
        {
            State = state.Value,
        }));
    }

    /// <summary>
    /// Производит запись объектов
    /// </summary>
    public static async ValueTask Write<T>(this IRowWriter writer, IEnumerable<T> objects, RowState state)
    {
        await writer.Write(objects.Select(o => new ObjectRowAdapter<T>()
        {
            State = state,
            Object = o
        }));
    }

    /// <summary>
    /// Производит запись строк из асинхронной последовательности
    /// </summary>
    public static async ValueTask<IRow?> Write(this IRowWriter writer, IAsyncEnumerable<IRow> rows, CancellationToken cancellationToken = default)
    {
        if (cancellationToken.IsCancellationRequested)
            return null;
        
        await foreach (var row in rows)
        {
            await writer.Write(row);
            
            if (cancellationToken.IsCancellationRequested)
                return row;
        }
        
        return null;
    }
    
    /// <summary>
    /// Перегоняет строки из читателя в писатель
    /// </summary>
    public static ValueTask<WritingResult> TryWriteFrom(this IRowWriter writer, DbDataReader reader, RowState rowState = RowState.Added, bool ownsReader = true, CancellationToken cancellationToken = default) => 
        writer.TryWriteFrom(reader.GetRowReader(rowState, ownsReader), cancellationToken);
    
    /// <summary>
    /// Перегоняет строки из читателя в писатель используя конвертер
    /// </summary>
    public static ValueTask<WritingResult> TryWriteFrom(this IRowWriter writer, IRowReader reader, CancellationToken cancellationToken = default) =>
        reader.TryWriteTo(writer, cancellationToken);
    
    /// <summary>
    /// Перегоняет строки из читателя в писатель
    /// </summary>
    public static ValueTask<IRow?> WriteFrom(this IRowWriter writer, DbDataReader reader, RowState rowState = RowState.Added, bool ownsReader = true, CancellationToken cancellationToken = default) => 
        writer.WriteFrom(reader.GetRowReader(rowState, ownsReader), cancellationToken);
    
    /// <summary>
    /// Перегоняет строки из читателя в писатель используя конвертер
    /// </summary>
    public static ValueTask<IRow?> WriteFrom(this IRowWriter writer, IRowReader reader, CancellationToken cancellationToken = default) =>
        reader.WriteTo(writer, cancellationToken);

    /// <summary>
    /// Возвращает писатель строк использующий конвертер значений
    /// </summary>
    public static IRowWriter UsingValueConverter(this IRowWriter writer, IValueConverter converter)
    {
        var tableWriter = writer as ITableWriter;
        if (tableWriter == null)
            throw new NotSupportedException("The writer does not support the ITableWriter.");
        
        return writer.UsingMapper(converter.CreateRowMapper(tableWriter.Table));
    }
    
    /// <summary>
    /// Возвращает писатель строк использующий маппер
    /// </summary>
    public static IRowWriter UsingMapper(this IRowWriter writer, IRowMapper mapper) =>
        writer.UsingConverter(row => new MappedRow(row, mapper));
    
    /// <summary>
    /// Возвращает писатель строк использующий конвертер
    /// </summary>
    public static IRowWriter UsingConverter(this IRowWriter writer, Func<IRow, IRow?> converter)
    {
        return new ConvertedRowWriter(writer, converter);
    }
}