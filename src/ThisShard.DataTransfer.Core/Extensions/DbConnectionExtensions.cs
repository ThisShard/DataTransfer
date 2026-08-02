using System.Data;
using System.Data.Common;
using ThisShard.Database.Core.Converters;
using ThisShard.Database.Core.Mappers;
using ThisShard.Database.Core.Models;
using ThisShard.Database.Core.Models.Rows;
using ThisShard.Database.Core.Options;
using ThisShard.Database.Core.Readers;
using ThisShard.Database.Core.Writers;

namespace ThisShard.Database.Core.Extensions;

public static class DbConnectionExtensions
{
    /// <summary>
    /// Действие с открытым соединением
    /// </summary>
    public static async ValueTask Execute<TConnection>(this TConnection connection, Func<ValueTask> action)
        where TConnection : DbConnection
    {
        var isConnectionOpen = connection.IsOpen();
        
        try
        {
            if (!isConnectionOpen)
                await connection.OpenAsync();
            
            await action();
        }
        finally
        {
            if (!isConnectionOpen)
                await connection.CloseAsync();
        }
    }

    /// <summary>
    /// Проверяет открыто ли соединение
    /// </summary>
    public static bool IsOpen(this DbConnection connection) => (connection.State & ConnectionState.Open) != 0;

    #region Write
    
    /// <summary>
    /// Писать данные в БД
    /// </summary>
    public static async ValueTask Write<TConnection>(this TConnection connection, 
        Func<TConnection, ValueTask<ITableWriter>> createWriter,
        Func<IRowWriter, ValueTask> writing)
        where TConnection : DbConnection =>
        await connection.Execute(async () =>
        {
            await using var writer = await createWriter(connection);
            await writing(writer);
            await writer.Complete();
        });
    
    #endregion
    
    #region GetSustainableRowReader

    /// <summary>
    /// Возвращает устойчивого читателя
    /// </summary>
    public static IRowReader GetSustainableRowReader<TConnection>(this TConnection connection,
        Func<TConnection, IRow?, IRowWriter, CancellationToken, Task> reading,
        SustainableOperationsOptions<TConnection>? options = null,
        bool ownsConnection = false)
        where TConnection : DbConnection
    {
        options ??= SustainableOperationsOptions<TConnection>.Default;

        return new SustainableDbRowReader<TConnection>(
            connection: connection,
            readerFactory: async (cn, row, retry) =>
            {
                if (options.MaxRetryCount > -1 && retry > options.MaxRetryCount)
                    return null;

                var delay = options.GetRetryDelay(retry);
                if (delay > 0)
                    await Task.Delay(delay);

                return new PipeReader((writer, ct) => reading(cn, row, writer, ct));
            },
            terminatePredicate: options.TerminatePredicate,
            ownsConnection: ownsConnection);
    }
    
    #endregion

    #region GetSustainableTableWriter

    /// <summary>
    /// Возвращает устойчивого писателя
    /// </summary>
    public static ITableWriter GetSustainableTableWriter<TConnection>(this TConnection connection,
        Func<TConnection, ITableWriter> factory,
        SustainableOperationsOptions<TConnection>? options = null,
        bool isInitialized = false,
        bool ownsConnection = false)
        where TConnection : DbConnection
    {
        options ??= SustainableOperationsOptions<TConnection>.Default;
        
        return new SustainableRelationalTableWriter<TConnection>(
            connection: connection, 
            writerFactory: async (cn, retry) =>
            {
                if (options.MaxRetryCount > -1 && retry > options.MaxRetryCount)
                    return null;

                var delay = options.GetRetryDelay(retry);
                if (delay > 0)
                    await Task.Delay(delay);

                return factory(cn);
            }, 
            terminatePredicate: options.TerminatePredicate, 
            isInnerWriterInitialized: isInitialized,
            rowConverterOnRetry: options.RowConverterOnRetry,
            ownsConnection: ownsConnection);
    }

    #endregion
}