using ThisShard.Database.Core.Models;
using ThisShard.Database.Core.Models.Rows;

namespace ThisShard.Database.Core.Writers;

/// <summary>
/// Буферизованный писатель в таблицу
/// </summary>
public abstract class BufferedTableWriter : BaseTableWriter
{
    /// <summary>
    /// Буфер строк
    /// </summary>
    protected List<IRow> Buffer { get; }
    
    /// <summary>
    /// Размер буфера
    /// </summary>
    // ReSharper disable once MemberCanBePrivate.Global
    protected int BufferSize { get; }
    
    /// <summary>
    /// Строки ожидающие обработку
    /// </summary>
    public override IEnumerable<IRow> PendingRows => Buffer;

    protected BufferedTableWriter(int bufferSize)
    {
        if (bufferSize <= 0)
            throw new ArgumentOutOfRangeException(nameof(bufferSize));
        
        BufferSize = bufferSize;
        Buffer = new List<IRow>(bufferSize);
    }

    /// <summary>
    /// Действие при записи строк
    /// </summary>
    protected override async ValueTask OnWrite(IEnumerable<IRow> rows)
    {
        foreach (var row in rows)
        {
            await Write(row);
        }
    }

    /// <summary>
    /// Действие при записи одиночной строки
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