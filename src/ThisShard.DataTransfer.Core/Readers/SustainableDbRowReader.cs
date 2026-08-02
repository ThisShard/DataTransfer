using System.Data.Common;
using ThisShard.Database.Core.Extensions;
using ThisShard.Database.Core.Models;
using ThisShard.Database.Core.Models.Rows;

namespace ThisShard.Database.Core.Readers;

/// <summary>
/// Устойчивый читатель строк
/// </summary>
public class SustainableDbRowReader<TConnection> : IRowReader
    where TConnection: DbConnection
{
    private readonly TConnection _connection;
    private readonly Func<TConnection, IRow?, int, ValueTask<IRowReader?>> _readerFactory;
    private readonly Func<TConnection, Exception, bool> _terminatePredicate;
    private readonly bool _ownsConnection;
    private readonly bool _shouldCloseConnection;

    private IRow? _lastReadRow;
    
    private IRowReader? _reader;
    
    public SustainableDbRowReader(
        TConnection connection, 
        Func<TConnection, IRow?, int, ValueTask<IRowReader?>> readerFactory, 
        Func<TConnection, Exception, bool> terminatePredicate, 
        bool ownsConnection = false)
    {
        _connection = connection ?? throw new ArgumentNullException(nameof(connection));
        _readerFactory = readerFactory ?? throw new ArgumentNullException(nameof(readerFactory));
        _terminatePredicate = terminatePredicate ?? throw new ArgumentNullException(nameof(terminatePredicate));
        _ownsConnection = ownsConnection;
        
        _shouldCloseConnection = !connection.IsOpen();
    }

    /// <summary>
    /// Читает следующую строку
    /// </summary>
    public ValueTask<IRow?> Read() => DoWithReader(async reader =>
    {
        var row = await reader.Read();
        _lastReadRow = row;
        return row;
    });

    /// <summary>Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources asynchronously.</summary>
    /// <returns>A task that represents the asynchronous dispose operation.</returns>
    public async ValueTask DisposeAsync()
    {
        await DisposeReader();

        if (_shouldCloseConnection)
            await _connection.CloseAsync();

        if (_ownsConnection)
            await _connection.DisposeAsync();
    }
    
    /// <summary>
    /// Выполняет действие с читателем
    /// </summary>
    private async ValueTask<TResult> DoWithReader<TResult>(Func<IRowReader, ValueTask<TResult>> func)
    {
        while (true)
        {
            var reader = await GetReader();
            try
            {
                return await func(reader);
            }
            catch (Exception ex)
            {
                await DisposeReader();
                
                if (_terminatePredicate(_connection, ex))
                    throw;
            }
        }
    }

    /// <summary>
    /// Диспозит читателя
    /// </summary>
    private async ValueTask DisposeReader()
    {
        if (_reader != null)
        {
            await _reader.DisposeAsync();
            _reader = null;
        }
    }

    /// <summary>
    /// Возвращает читателя
    /// </summary>
    private async ValueTask<IRowReader> GetReader()
    {
        if (_reader != null)
            return _reader;

        var result = await CreateReader();
        if (result == null)
            throw new InvalidOperationException("Could not create reader");

        _reader = result;
        
        return _reader;
    }

    /// <summary>
    /// Создает читателя из фабрики
    /// </summary>
    private async ValueTask<IRowReader?> CreateReader()
    {
        var retryCount = 0;
        while (true)
        {
            try
            {
                if (!_connection.IsOpen())
                    await _connection.OpenAsync();
                
                return await _readerFactory(_connection, _lastReadRow, retryCount);
            }
            catch (Exception ex)
            {
                if (_terminatePredicate(_connection, ex))
                    throw;
                
                retryCount++;
            }
        }
    }
}