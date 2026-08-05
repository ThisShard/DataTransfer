using ThisShard.Database.Core.Models.Tables;
using ThisShard.Database.Core.Writers;

namespace ThisShard.Database.Core.Pipelines;

/// <summary>
/// Назначение данных для конвейера
/// </summary>
public class PipelineDestination<TConnection> : IPipelineDestination
    where TConnection : class
{
    private readonly bool _ownsConnection;
    private readonly Func<ValueTask<TConnection>> _connectionFactory;
    private readonly Func<TConnection, ValueTask<IRowWriter>> _writerFactory;
    private readonly Func<TConnection, ITable, ValueTask>? _initFunc;

    private TConnection? _connection;
    
    /// <summary>
    /// Ключ
    /// </summary>
    public string Key { get; }

    public PipelineDestination(
        string key, 
        Func<ValueTask<TConnection>> connectionFactory, 
        Func<TConnection, ValueTask<IRowWriter>> writerFactory, 
        Func<TConnection, ITable, ValueTask>? initFunc = null, 
        bool ownsConnection = true
        )
    {
        Key = key ?? throw new ArgumentNullException(nameof(key));
        _connectionFactory = connectionFactory ?? throw new ArgumentNullException(nameof(connectionFactory));
        _writerFactory = writerFactory ?? throw new ArgumentNullException(nameof(writerFactory));
        _initFunc = initFunc;
        _ownsConnection = ownsConnection;
    }

    /// <summary>
    /// Инициализирует таблицей
    /// </summary>
    public async ValueTask Init(ITable table)
    {
        if (_initFunc == null)
            return;
        
        _connection ??= await _connectionFactory();
        await _initFunc(_connection, table);
    }

    /// <summary>
    /// Возвращает писателя
    /// </summary>
    public async ValueTask<IRowWriter> GetWriter()
    {
        _connection ??= await _connectionFactory();
        return await _writerFactory(_connection);
    }

    /// <summary>Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources asynchronously.</summary>
    /// <returns>A task that represents the asynchronous dispose operation.</returns>
    public async ValueTask DisposeAsync()
    {
        if (_connection == null)
            return;

        if (!_ownsConnection)
        {
            _connection = null;
            return;
        }
        
        if (_connection is IAsyncDisposable asyncDisposable)
            await asyncDisposable.DisposeAsync();
        else if (_connection is IDisposable disposable)
            disposable.Dispose();

        _connection = null;
    }
}