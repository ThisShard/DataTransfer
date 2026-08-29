using ThisShard.Database.Core.Models.Rows;
using ThisShard.Database.Core.Models.Tables;
using ThisShard.Database.Core.Readers;

namespace ThisShard.Database.Core.Pipelines.Sources;

/// <summary>
/// Источник данных для конвейера
/// </summary>
public class PipelineSource<TConnection> : IPipelineSource
    where TConnection : class
{
    /// <summary>
    /// Владеет соединением
    /// </summary>
    protected bool OwnsConnection { get; }
    
    /// <summary>
    /// Фабрика соединений
    /// </summary>
    protected Func<ValueTask<TConnection>> ConnectionFactory { get; }
    
    /// <summary>
    /// Фабрика ридеров
    /// </summary>
    protected Func<TConnection, IRow?, ValueTask<IRowReader>> ReaderFactory { get; }
    
    /// <summary>
    /// Геттер таблиц
    /// </summary>
    protected Func<TConnection, ValueTask<ITable?>>? TableGetter { get; }

    /// <summary>
    /// Текщуее соединение
    /// </summary>
    protected TConnection? Connection { get; set; }
    
    /// <summary>
    /// Ключ
    /// </summary>
    public string Key { get; }

    public PipelineSource(
        string key, 
        Func<ValueTask<TConnection>> connectionFactory, 
        Func<TConnection, IRow?, ValueTask<IRowReader>> readerFactory, 
        Func<TConnection, ValueTask<ITable?>>? tableGetter = null, 
        bool ownsConnection = true
    )
    {
        Key = key ?? throw new ArgumentNullException(nameof(key));
        ConnectionFactory = connectionFactory ?? throw new ArgumentNullException(nameof(connectionFactory));
        ReaderFactory = readerFactory ?? throw new ArgumentNullException(nameof(readerFactory));
        TableGetter = tableGetter;
        OwnsConnection = ownsConnection;
    }

    /// <summary>
    /// Возвращает таблицу
    /// </summary>
    public async ValueTask<ITable?> GetTable()
    {
        if (TableGetter == null)
            return null;
        
        Connection ??= await CreateConnection();
        return await TableGetter(Connection);
    }

    /// <summary>
    /// Возвращает читателя
    /// </summary>
    public async ValueTask<IRowReader> GetReader(IRow? lastWrittenRow = null)
    {
        Connection ??= await CreateConnection();
        return await ReaderFactory(Connection, lastWrittenRow);
    }
    
    /// <summary>Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources asynchronously.</summary>
    /// <returns>A task that represents the asynchronous dispose operation.</returns>
    public async ValueTask DisposeAsync()
    {
        if (Connection == null)
            return;

        await CloseConnection();
        
        if (!OwnsConnection)
        {
            Connection = null;
            return;
        }
        
        await DisposeConnection();

        Connection = null;
    }

    /// <summary>
    /// Создает соединение
    /// </summary>
    protected virtual ValueTask<TConnection> CreateConnection() => ConnectionFactory();

    /// <summary>
    /// Закрывает соединение
    /// </summary>
    protected virtual async ValueTask CloseConnection()
    {
    }
    
    /// <summary>
    /// Диспозит соединение
    /// </summary>
    protected virtual async ValueTask DisposeConnection()
    {
        if (Connection is IAsyncDisposable asyncDisposable)
            await asyncDisposable.DisposeAsync();
        else if (Connection is IDisposable disposable)
            disposable.Dispose();
    }
}