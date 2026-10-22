using ThisShard.Database.Models.Tables;
using ThisShard.Database.Writers;

namespace ThisShard.Database.Pipelines.Destinations;

/// <summary>
/// Назначение данных для конвейера
/// </summary>
public class PipelineDestination<TConnection> : IPipelineDestination
    where TConnection : class
{
    protected bool OwnsConnection { get; }
    protected Func<ValueTask<TConnection>> ConnectionFactory { get; }
    protected Func<TConnection, ITable?, ValueTask<IWriter>> WriterFactory { get; }
    protected Func<TConnection, ITable?, ValueTask>? InitFunc { get; }

    protected TConnection? Connection { get; set; }
    protected ITable? Table { get; set; }
    
    /// <summary>
    /// Ключ
    /// </summary>
    public string Key { get; }
    
    public PipelineDestination(
        string key,
        Func<ValueTask<TConnection>> connectionFactory,
        Func<TConnection, ValueTask<IWriter>> writerFactory,
        Func<TConnection, ITable, ValueTask>? initFunc = null,
        bool ownsConnection = true
    ) : this(
        key,
        connectionFactory,
        (writerFactory != null! ? 
            (cn, _) => writerFactory(cn) 
            : null)!,
        initFunc,
        ownsConnection)
    {
    }
    
    public PipelineDestination(
        string key, 
        Func<ValueTask<TConnection>> connectionFactory, 
        Func<TConnection, ITable?, ValueTask<IWriter>> writerFactory, 
        Func<TConnection, ITable?, ValueTask>? initFunc = null, 
        bool ownsConnection = true
        )
    {
        Key = key ?? throw new ArgumentNullException(nameof(key));
        ConnectionFactory = connectionFactory ?? throw new ArgumentNullException(nameof(connectionFactory));
        WriterFactory = writerFactory ?? throw new ArgumentNullException(nameof(writerFactory));
        InitFunc = initFunc;
        OwnsConnection = ownsConnection;
    }

    /// <summary>
    /// Инициализирует таблицей
    /// </summary>
    public async ValueTask Init(ITable? table)
    {
        Table = table;
        
        if (InitFunc == null)
            return;

        Connection ??= await CreateConnection();
        await InitFunc(Connection, table);
    }

    /// <summary>
    /// Возвращает писателя
    /// </summary>
    public async ValueTask<IWriter> GetWriter()
    {
        Connection ??= await CreateConnection();
        return await WriterFactory(Connection, Table);
    }

    /// <summary>Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources asynchronously.</summary>
    /// <returns>A task that represents the asynchronous dispose operation.</returns>
    public async ValueTask DisposeAsync()
    {
        if (Connection == null)
            return;

        if (!OwnsConnection)
        {
            Connection = null;
            return;
        }
        
        if (Connection is IAsyncDisposable asyncDisposable)
            await asyncDisposable.DisposeAsync();
        else if (Connection is IDisposable disposable)
            disposable.Dispose();

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