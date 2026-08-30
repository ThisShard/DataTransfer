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
    private const string IndexKey = "__RowReaderExtensions_Index";
    private const string PreviousRowKey = "__RowReaderExtensions_PreviousRow";
    
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
        IRow? lastRow = null;
        
        try
        {
            var _isCanceled = false;
            var index = 0l;
            
            while(true)
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    _isCanceled = true;
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
                State = _isCanceled ? WritingState.Canceled : WritingState.Success,
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
        IReadOnlyCollection<IRowWriter> writers)
    {
        var rowsToSearch = writers
            .SelectMany(x => x.PendingRows);
        
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