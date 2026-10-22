using ThisShard.Database.Models.Results;
using ThisShard.Database.Models.Rows;
using ThisShard.Database.Readers;
using ThisShard.Database.Writers;

namespace ThisShard.Database.Extensions;

/// <summary>
/// Расширения для IReader
/// </summary>
public static class ReaderExtensions
{
    private const string IndexKey = "__RowReaderExtensions_Index";
    private const string PreviousRowKey = "__RowReaderExtensions_PreviousRow";
    
    /// <summary>
    /// Перегоняет строки из ридера в писатели
    /// </summary>
    public static ValueTask<IRow?> WriteTo(this IReader reader, IWriter writer, CancellationToken cancellationToken = default) =>
        reader.WriteTo([writer], cancellationToken);

    /// <summary>
    /// Перегоняет строки из ридера в писатели
    /// </summary>
    public static async ValueTask<IRow?> WriteTo(this IReader reader, IReadOnlyCollection<IWriter> writers,
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
    public static ValueTask<WritingResult> TryWriteTo(this IReader reader, IWriter writer, CancellationToken cancellationToken = default) =>
        reader.TryWriteTo([writer], cancellationToken);
    
    /// <summary>
    /// Пытается произвести запись из ридера в писатели
    /// </summary>
    public static async ValueTask<WritingResult> TryWriteTo(this IReader reader, IReadOnlyCollection<IWriter> writers, CancellationToken cancellationToken = default)
    {   
        IRow? lastRow = null;
        
        try
        {
            var isCanceled = false;
            var index = 0L;
            
            while(true)
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    isCanceled = true;
                    break;
                }
                
                var row = await reader.Read();
                if (row == null)
                    break;

                SetMetadata(row, index++, lastRow);
                
                foreach (var writer in writers)
                    await writer.Write(row);
            
                lastRow = row;
            }

            foreach (var writer in writers)
                await writer.Flush();

            return new WritingResult
            {
                State = isCanceled ? WritingState.Canceled : WritingState.Success,
                LastWrittenRow = GetLastWrittenRow(lastRow, writers),
                Reader = reader,
                Writers = writers
            };
        }
        catch (Exception ex)
        {
            return new WritingResult()
            {
                State = WritingState.Error,
                Exception = ex,
                LastWrittenRow = GetLastWrittenRow(lastRow, writers),
                Reader = reader,
                Writers = writers
            };
        }
    }

    /// <summary>
    /// Возвращает последнюю записанную строку
    /// </summary>
    private static IRow? GetLastWrittenRow(IRow? lastRow,
        IReadOnlyCollection<IWriter> writers)
    {
        var rowsToSearch = writers
            .SelectMany(x => x.PeekBuffer());
        
        if (lastRow != null)
            rowsToSearch = rowsToSearch.Concat([lastRow]);

        var lastWrittenRow = rowsToSearch
            .Select(GetMetadata)
            .DefaultIfEmpty((0, null))
            .MinBy(x => x.Index);

        return lastWrittenRow.PreviousRow;
    }

    /// <summary>
    /// Получает метаданные
    /// </summary>
    private static (long Index, IRow? PreviousRow) GetMetadata(IRow row)
    {
        return ((long)row.Metadata[IndexKey]!, row.Metadata[PreviousRowKey] as IRow);
    }

    /// <summary>
    /// Устанавливает метаданные
    /// </summary>
    private static void SetMetadata(IRow row, long index, IRow? previousRow)
    {
        row.Metadata[IndexKey] = index;
        row.Metadata[PreviousRowKey] = previousRow;
    }
    
    /// <summary>
    /// Формирует IAsyncEnumerable из ридера
    /// </summary>
    public static async IAsyncEnumerable<IRow> AsAsyncEnumerable(this IReader reader)
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
    public static async ValueTask<List<IRow>> ReadToEnd(this IReader reader)
    {
        var result = new List<IRow>();
        await foreach (var row in reader.AsAsyncEnumerable())
        {
            result.Add(row);
        }
        return result;
    }
}