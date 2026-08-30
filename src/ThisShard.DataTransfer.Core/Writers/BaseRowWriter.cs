using ThisShard.Database.Core.Models;
using ThisShard.Database.Core.Models.Rows;
using ThisShard.Database.Core.Models.Tables;

namespace ThisShard.Database.Core.Writers;

/// <summary>
/// Базовый писатель данных
/// </summary>
public abstract class BaseRowWriter : IRowWriter
{
    /// <summary>
    /// Состояние писателя
    /// </summary>
    public WriterState State { get; protected set; } = WriterState.Initialized;

    /// <summary>
    /// Строки ожидающие обработку
    /// </summary>
    public virtual IEnumerable<IRow> PendingRows => Enumerable.Empty<IRow>();

    /// <summary>
    /// Записывает множество строк
    /// </summary>
    public async ValueTask Write(IEnumerable<IRow> rows)
    {
        if (State == WriterState.Writing)
        {
            await OnWrite(rows);
            return;
        }
        
        if (State != WriterState.Initialized)
            throw new InvalidOperationException();
        
        await OnWrite(rows);
        
        State = WriterState.Writing;
    }

    /// <summary>
    /// Записывает строку
    /// </summary>
    public async ValueTask Write(IRow row)
    {
        if (State == WriterState.Writing)
        {
            await OnWrite(row);
            return;
        }
        
        if (State != WriterState.Initialized)
            throw new InvalidOperationException();
        
        await OnWrite(row);
        
        State = WriterState.Writing;
    }

    /// <summary>
    /// Принудительно производит запись
    /// </summary>
    public async ValueTask Flush()
    {
        if (State != WriterState.Writing && State != WriterState.Initialized)
            throw new InvalidOperationException();

        await OnFlush();
    }

    /// <summary>
    /// Завершает запись
    /// </summary>
    public async ValueTask Complete()
    {
        if (State != WriterState.Writing && State != WriterState.Initialized)
            throw new InvalidOperationException();
        
        await OnComplete();

        State = WriterState.Completed;
    }

    /// <summary>Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources asynchronously.</summary>
    /// <returns>A task that represents the asynchronous dispose operation.</returns>
    public virtual async ValueTask DisposeAsync()
    {
        await OnDispose();
        
        State = WriterState.Disposed;
    }
    
    #region Abstract

    /// <summary>
    /// Действие при записи строк
    /// </summary>
    protected abstract ValueTask OnWrite(IEnumerable<IRow> rows);
    
    /// <summary>
    /// Действие при записи одиночной строки
    /// </summary>
    protected abstract ValueTask OnWrite(IRow row);
    
    /// <summary>
    /// Действие при принудительной записи
    /// </summary>
    protected abstract ValueTask OnFlush();
    
    /// <summary>
    /// Действие при завершении записи
    /// </summary>
    protected abstract ValueTask OnComplete();
    
    /// <summary>
    /// Действие при очистке
    /// </summary>
    protected abstract ValueTask OnDispose();
    
    #endregion

}