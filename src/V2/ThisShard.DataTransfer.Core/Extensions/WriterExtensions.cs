using System.Data;
using System.Data.Common;
using ThisShard.Database.Mappers;
using ThisShard.Database.Models.Rows;
using ThisShard.Database.Models.Tables;
using ThisShard.Database.Readers;
using ThisShard.Database.Writers;
using ThisShard.Database.Writers.States;
using ThisShard.Foundation.Converters;

namespace ThisShard.Database.Extensions;

/// <summary>
/// Расширения для райтера
/// </summary>
public static class WriterExtensions
{
    /// <summary>
    /// Производит запись DataTable
    /// </summary>
    public static ValueTask Write(this IWriter writer, DataTable table, RowState? state = null) => 
        writer.Write(table.Rows.OfType<DataRow>(), state);

    /// <summary>
    /// Производит запись DataRow
    /// </summary>
    public static async ValueTask Write(this IWriter writer, IEnumerable<DataRow> rows, RowState? state = null)
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
    public static async ValueTask Write<T>(this IWriter writer, IEnumerable<T> objects, RowState state)
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
    public static async ValueTask<IRow?> Write(this IWriter writer, IAsyncEnumerable<IRow> rows, CancellationToken cancellationToken = default)
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
    public static ValueTask<WritingResult> TryWriteFrom(this IWriter writer, DbDataReader reader, RowState rowState = RowState.Added, bool ownsReader = true, CancellationToken cancellationToken = default) => 
        writer.TryWriteFrom(reader.GetRowReader(rowState, ownsReader), cancellationToken);
    
    /// <summary>
    /// Перегоняет строки из читателя в писатель используя конвертер
    /// </summary>
    public static ValueTask<WritingResult> TryWriteFrom(this IWriter writer, IReader reader, CancellationToken cancellationToken = default) =>
        reader.TryWriteTo(writer, cancellationToken);
    
    /// <summary>
    /// Перегоняет строки из читателя в писатель
    /// </summary>
    public static ValueTask<IRow?> WriteFrom(this IWriter writer, DbDataReader reader, RowState rowState = RowState.Added, bool ownsReader = true, CancellationToken cancellationToken = default) => 
        writer.WriteFrom(reader.GetRowReader(rowState, ownsReader), cancellationToken);
    
    /// <summary>
    /// Перегоняет строки из читателя в писатель используя конвертер
    /// </summary>
    public static ValueTask<IRow?> WriteFrom(this IWriter writer, IReader reader, CancellationToken cancellationToken = default) =>
        reader.WriteTo(writer, cancellationToken);
    
    /// <summary>
    /// Возвращает писатель строк использующий конвертер значений
    /// </summary>
    public static IWriter UsingValueConverter(this IWriter writer, ITable table, IValueConverter converter)
    {
        return writer.UsingMapper(converter.CreateRowMapper(table));
    }
    
    /// <summary>
    /// Возвращает писатель строк использующий маппер
    /// </summary>
    public static IWriter UsingMapper(this IWriter writer, IRowMapper mapper) =>
        writer.UsingMapper(row => new MappedRow(row, mapper));
    
    /// <summary>
    /// Возвращает райтер строк использующий функцию мэппинга
    /// </summary>
    public static IWriter UsingMapper(this IWriter writer, Func<IRow, IRow?> mapper) => 
        new MappedWriter(writer, mapper);
    
    /// <summary>
    /// Записывает множество строк
    /// </summary>
    public static async ValueTask Write(this IWriter writer, IEnumerable<IRow> rows)
    {
        foreach (var row in rows)
        {
            await writer.Write(row);
        }
    }
}