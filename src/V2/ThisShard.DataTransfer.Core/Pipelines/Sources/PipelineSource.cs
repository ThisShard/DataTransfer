using ThisShard.Database.Models.Rows;
using ThisShard.Database.Models.Tables;
using ThisShard.Database.Readers;

namespace ThisShard.Database.Pipelines.Sources;

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
    protected Func<TConnection, IRow?, ValueTask<IReader>> ReaderFactory { get; }
    
    /// <summary>
    /// Геттер таблиц
    /// </summary>
    protected Func<TConnection, ValueTask<ITable>> TableGetter { get; }

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
        Func<TConnection, IRow?, ValueTask<IReader>> readerFactory, 
        Func<TConnection, ValueTask<ITable>> tableGetter,
        bool ownsConnection = true
    )
    {
        Key = key ?? throw new ArgumentNullException(nameof(key));
        ConnectionFactory = connectionFactory ?? throw new ArgumentNullException(nameof(connectionFactory));
        ReaderFactory = readerFactory ?? throw new ArgumentNullException(nameof(readerFactory));
        TableGetter = tableGetter ?? throw new ArgumentNullException(nameof(tableGetter));
        OwnsConnection = ownsConnection;
    }

    /// <summary>
    /// Возвращает таблицу
    /// </summary>
    public async ValueTask<ITable> GetTable()
    {
        Connection ??= await CreateConnection();
        return await TableGetter(Connection);
    }

    /// <summary>
    /// Возвращает читателя
    /// </summary>
    public async ValueTask<IReader> GetReader(IRow? lastWrittenRow = null)
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