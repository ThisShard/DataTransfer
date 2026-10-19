using ThisShard.Database.Models.Rows;

namespace ThisShard.Database.Writers;

/// <summary>
/// Райтер с поддержкой буферизации строк
/// </summary>
public abstract class BufferedWriter : BaseWriter
{
    /// <summary>
    /// Буфер строк
    /// </summary>
    protected List<IRow> Buffer { get; }
    
    /// <summary>
    /// Размер буфера
    /// </summary>
    // ReSharper disable once MemberCanBePrivate.Global
    public override int BufferSize { get; }

    protected BufferedWriter(int bufferSize)
    {
        if (bufferSize <= 0)
            throw new ArgumentOutOfRangeException(nameof(bufferSize));
        
        BufferSize = bufferSize;
        Buffer = new List<IRow>(bufferSize);
    }

    /// <summary>
    /// Вернуть буфер строк без извлечения
    /// </summary>
    public override IEnumerable<IRow> PeekBuffer() => Buffer;

    /// <summary>
    /// Вернуть буфер строк с извлечением
    /// </summary>
    public override IReadOnlyList<IRow> PopBuffer()
    {
        var popped = Buffer.ToArray();
        Buffer.Clear();
        return popped;
    }

    /// <summary>
    /// Действие при записи строки
    /// </summary>
    protected override async ValueTask OnWrite(IRow row)
    {
        if (!ShouldAddRowToBuffer(row))
            return;
        
        Buffer.Add(row);

        if (Buffer.Count < BufferSize)
            return;
        
        await Flush();
    }

    /// <summary>
    /// Проверка на то что нужно ли добавлять строку в буфер
    /// </summary>
    protected virtual bool ShouldAddRowToBuffer(IRow row) => row.State != RowState.Ignored;
    
    /// <summary>
    /// Производит очистку буфера
    /// </summary>
    protected virtual void ClearBuffer() => Buffer.Clear();
}