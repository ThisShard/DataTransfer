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

    public async ValueTask Flush()
    {
        await _innerWriter.Flush();
    }
}