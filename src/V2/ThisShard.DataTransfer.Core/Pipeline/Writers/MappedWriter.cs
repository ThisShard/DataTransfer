using ThisShard.Database.Models.Rows;

namespace ThisShard.Database.Pipeline.Writers;

/// <summary>
/// Враппер над райтером, который маппит строки перед передачей их внутреннему райтеру
/// </summary>
public class MappedWriter : IWriter
{
    private readonly IWriter _writer;
    private readonly Func<IRow, IRow?> _mapper;

    /// <summary>
    /// Статус
    /// </summary>
    public WriterStatus Status => _writer.Status;

    /// <summary>
    /// Размер буфера
    /// </summary>
    public int BufferSize => _writer.BufferSize;

    public MappedWriter(IWriter writer, Func<IRow, IRow?> mapper)
    {
        ArgumentNullException.ThrowIfNull(writer);
        ArgumentNullException.ThrowIfNull(mapper);

        _writer = writer;
        _mapper = mapper;
    }

    /// <summary>
    /// Вернуть буфер строк без извлечения
    /// </summary>
    public IEnumerable<IRow> PeekBuffer() => _writer.PeekBuffer();

    /// <summary>
    /// Вернуть буфер строк с извлечением
    /// </summary>
    public IReadOnlyList<IRow> PopBuffer() => _writer.PopBuffer();

    /// <summary>
    /// Производит инициализацию
    /// </summary>
    public ValueTask Init() => _writer.Init();

    /// <summary>
    /// Записывает строку
    /// </summary>
    public async ValueTask Write(IRow row)
    {
        var convertedRow = _mapper(row);
        if (convertedRow == null)
            return;

        await _writer.Write(row);
    }

    /// <summary>
    /// Принудительно производит запись
    /// </summary>
    public ValueTask Flush() => _writer.Flush();

    /// <summary>
    /// Завершает запись
    /// </summary>
    public ValueTask Complete() => _writer.Complete();

    /// <summary>Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources asynchronously.</summary>
    /// <returns>A task that represents the asynchronous dispose operation.</returns>
    public ValueTask DisposeAsync() => _writer.DisposeAsync();
}