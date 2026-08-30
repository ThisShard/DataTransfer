using ThisShard.Database.Core.Models;
using ThisShard.Database.Core.Models.Rows;

namespace ThisShard.Database.Core.Writers;

/// <summary>
/// Писатель строк с использованием конвертера
/// </summary>
public class ConvertedRowWriter : IRowWriter
{
    private readonly IRowWriter _innerWriter;
    private readonly Func<IRow, IRow?> _converter;

    /// <summary>
    /// Состояние писателя
    /// </summary>
    public WriterState State => _innerWriter.State;

    /// <summary>
    /// Строки ожидающие обработку
    /// </summary>
    public IEnumerable<IRow> PendingRows => _innerWriter.PendingRows;

    public ConvertedRowWriter(IRowWriter writer, Func<IRow, IRow?> converter)
    {
        _innerWriter = writer ?? throw new ArgumentNullException(nameof(writer));
        _converter = converter ?? throw new ArgumentNullException(nameof(converter));
    }

    /// <summary>
    /// Записывает множество строк
    /// </summary>
    public async ValueTask Write(IEnumerable<IRow> rows)
    {
        await _innerWriter.Write(rows.Select(_converter).Where(x=>x != null)!);
    }

    /// <summary>
    /// Записывает строку
    /// </summary>
    public async ValueTask Write(IRow row)
    {
        var convertedRow = _converter(row);
        if (convertedRow == null)
            return;
        
        await _innerWriter.Write(convertedRow);
    }

    /// <summary>
    /// Принудительно производит запись
    /// </summary>
    public async ValueTask Flush()
    {
        await _innerWriter.Flush();
    }

    /// <summary>
    /// Завершает запись
    /// </summary>
    public async ValueTask Complete()
    {
        await _innerWriter.Complete();
    }

    /// <summary>Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources asynchronously.</summary>
    /// <returns>A task that represents the asynchronous dispose operation.</returns>
    public async ValueTask DisposeAsync()
    {
        await _innerWriter.DisposeAsync();
    }
}