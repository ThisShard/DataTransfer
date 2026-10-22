using ThisShard.Database.Models.Rows;
using ThisShard.Database.Writers.States;

namespace ThisShard.Database.Writers;

/// <summary>
/// Базовый райтер
/// </summary>
public abstract class BaseWriter : IWriter
{
    #region State

    /// <summary>
    /// Статус писателя
    /// </summary>
    public WriterStatus Status { get; private set; } = WriterStatus.Created;

    /// <summary>
    /// Размер буфера
    /// </summary>
    public virtual int BufferSize => 1;

    /// <summary>
    /// Вернуть буфер строк без извлечения
    /// </summary>
    public virtual IEnumerable<IRow> PeekBuffer() => Enumerable.Empty<IRow>();

    /// <summary>
    /// Вернуть буфер строк с извлечением
    /// </summary>
    public virtual IReadOnlyList<IRow> PopBuffer() => Array.Empty<IRow>();

    #endregion
    
    #region Lifetime
    
    /// <summary>
    /// Производит инициализацию
    /// </summary>
    public async ValueTask Init()
    {
        if (Status != WriterStatus.Created)
            throw new InvalidOperationException($"Writer must be in status {WriterStatus.Created}");
        
        await OnInit();
        
        Status = WriterStatus.Initialized;
    }
    
    /// <summary>
    /// Записывает строку
    /// </summary>
    public async ValueTask Write(IRow row)
    {
        if (Status == WriterStatus.Writing)
        {
            await OnWrite(row);
            return;
        }
        
        if (Status != WriterStatus.Initialized)
            throw new InvalidOperationException($"Writer must be in either {WriterStatus.Writing} or {WriterStatus.Initialized} status");
        
        await OnWrite(row);
        
        Status = WriterStatus.Writing;
    }

    /// <summary>
    /// Принудительно производит запись
    /// </summary>
    public async ValueTask Flush()
    {
        if (Status != WriterStatus.Writing && Status != WriterStatus.Initialized)
            throw new InvalidOperationException($"Writer must be in either {WriterStatus.Writing} or {WriterStatus.Initialized} status");

        await OnFlush();
    }

    /// <summary>
    /// Завершает запись
    /// </summary>
    public async ValueTask Complete()
    {
        if (Status != WriterStatus.Writing && Status != WriterStatus.Initialized)
            throw new InvalidOperationException($"Writer must be in either {WriterStatus.Writing} or {WriterStatus.Initialized} status");
        
        await OnComplete();

        Status = WriterStatus.Completed;
    }

    /// <summary>
    /// Производит очистку
    /// </summary>
    public virtual async ValueTask DisposeAsync()
    {
        if (Status == WriterStatus.Disposed)
            return;
        
        await OnDispose();
        
        Status = WriterStatus.Disposed;
    }

    #endregion
    
    #region Abstract

    /// <summary>
    /// Действие при инициализации
    /// </summary>
    protected abstract ValueTask OnInit();
    
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