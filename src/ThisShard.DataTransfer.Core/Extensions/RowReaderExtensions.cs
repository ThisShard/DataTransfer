using ThisShard.Database.Core.Models;
using ThisShard.Database.Core.Models.Results;
using ThisShard.Database.Core.Models.Rows;
using ThisShard.Database.Core.Readers;
using ThisShard.Database.Core.Writers;

namespace ThisShard.Database.Core.Extensions;

/// <summary>
/// Расширения для IRowReader
/// </summary>
public static class RowReaderExtensions
{
    /// <summary>
    /// Перегоняет строки из ридера в писатели
    /// </summary>
    public static ValueTask<IRow?> WriteTo(this IRowReader reader, IRowWriter writer, CancellationToken cancellationToken = default) =>
        reader.WriteTo([writer], cancellationToken);

    /// <summary>
    /// Перегоняет строки из ридера в писатели
    /// </summary>
    public static async ValueTask<IRow?> WriteTo(this IRowReader reader, IReadOnlyCollection<IRowWriter> writers,
        CancellationToken cancellationToken = default)
    {
        var result = await TryWriteTo(reader, writers, cancellationToken);
        
        if (result.Exception != null)
            throw result.Exception;

        return result.State == WritingState.Success 
            ? null 
            : result.LastWrittenRow;
    }
    
    /// <summary>
    /// Перегоняет строки из читателя в ридера используя конвертер
    /// </summary>
    public static ValueTask<WritingResult> TryWriteTo(this IRowReader reader, IRowWriter writer, CancellationToken cancellationToken = default) =>
        reader.TryWriteTo([writer], cancellationToken);
    
    /// <summary>
    /// Пытается произвести запись из ридера в писатели
    /// </summary>
    public static async ValueTask<WritingResult> TryWriteTo(this IRowReader reader, IReadOnlyCollection<IRowWriter> writers, CancellationToken cancellationToken = default)
    {   
        IRow? lastWrittenRow = null;
        
        try
        {
            while(true)
            {
                if (cancellationToken.IsCancellationRequested)
                    return new WritingResult()
                    {
                        State = WritingState.Canceled,
                        LastWrittenRow = lastWrittenRow,
                        Reader = reader,
                        Writers = writers
                    };
                
                var row = await reader.Read();
                if (row == null)
                    return new WritingResult()
                    {
                        State = WritingState.Success,
                        LastWrittenRow = lastWrittenRow,
                        Reader = reader,
                        Writers = writers
                    };

                foreach (var writer in writers)
                    await writer.Write(row);
            
                lastWrittenRow = row;
            }
        }
        catch (Exception ex)
        {
            return new WritingResult()
            {
                State = WritingState.Error,
                Exception = ex,
                LastWrittenRow = lastWrittenRow,
                Reader = reader,
                Writers = writers
            };
        }
    }
    
    /// <summary>
    /// Формирует IAsyncEnumerable из ридера
    /// </summary>
    public static async IAsyncEnumerable<IRow> AsAsyncEnumerable(this IRowReader reader)
    {
        while(true)
        {
            var row = await reader.Read();
            if (row == null)
                yield break;
            
            yield return row;
        }
    }

    /// <summary>
    /// Читает строки из ридера до конца
    /// </summary>
    public static async ValueTask<List<IRow>> ReadToEnd(this IRowReader reader)
    {
        var result = new List<IRow>();
        await foreach (var row in reader.AsAsyncEnumerable())
        {
            result.Add(row);
        }
        return result;
    }
}