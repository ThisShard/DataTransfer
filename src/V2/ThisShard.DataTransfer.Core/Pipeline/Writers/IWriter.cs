using ThisShard.Database.Schema.Rows;

namespace ThisShard.Database.Pipeline.Writers;

/// <summary>
/// Интерфейс райтера
/// </summary>
public interface IWriter : IAsyncDisposable
{
    /// <summary>
    /// Статус
    /// </summary>
    WriterStatus Status { get; }
    
    /// <summary>
    /// Размер буфера
    /// </summary>
    int BufferSize { get; }

    /// <summary>
    /// Вернуть буфер строк без извлечения
    /// </summary>
    IEnumerable<IRow> PeekBuffer();
    
    /// <summary>
    /// Вернуть буфер строк с извлечением
    /// </summary>
    IReadOnlyList<IRow> PopBuffer();

    /// <summary>
    /// Производит инициализацию
    /// </summary>
    ValueTask Init();
    
    /// <summary>
    /// Записывает строку
    /// </summary>
    ValueTask Write(IRow row);
    
    /// <summary>
    /// Принудительно производит запись
    /// </summary>
    ValueTask Flush();

    /// <summary>
    /// Завершает запись
    /// </summary>
    ValueTask Complete();
}