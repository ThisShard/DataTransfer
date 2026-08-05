using ThisShard.Database.Core.Models.Rows;
using ThisShard.Database.Core.Models.Tables;
using ThisShard.Database.Core.Readers;

namespace ThisShard.Database.Core.Pipelines;

/// <summary>
/// Источник данных для конвейера
/// </summary>
public class PipelineSource<TConnection> : IPipelineSource
    where TConnection : class
{
    private readonly bool _ownsConnection;
    private readonly Func<ValueTask<TConnection>> _connectionFactory;
    private readonly Func<TConnection, IRow?, ValueTask<IRowReader>> _readerFactory;
    private readonly Func<TConnection, ValueTask<ITable?>>? _tableGetter;

    private TConnection? _connection;
    
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
        _connectionFactory = connectionFactory ?? throw new ArgumentNullException(nameof(connectionFactory));
        _readerFactory = readerFactory ?? throw new ArgumentNullException(nameof(readerFactory));
        _tableGetter = tableGetter;
        _ownsConnection = ownsConnection;
    }

    /// <summary>
    /// Возвращает таблицу
    /// </summary>
    public async ValueTask<ITable?> GetTable()
    {
        if (_tableGetter == null)
            return null;
        
        _connection ??= await _connectionFactory();
        return await _tableGetter(_connection);
    }

    /// <summary>
    /// Возвращает читателя
    /// </summary>
    public async ValueTask<IRowReader> GetReader(IRow? lastWrittenRow = null)
    {
        _connection ??= await _connectionFactory();
        return await _readerFactory(_connection, lastWrittenRow);
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