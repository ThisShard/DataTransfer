using System.Data;
using System.Data.Common;
using ThisShard.Database.Models.Rows;
using ThisShard.Database.Options;
using ThisShard.Database.Readers;
using ThisShard.Database.Writers;

namespace ThisShard.Database.Extensions;

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
        Func<TConnection, ValueTask<IWriter>> createWriter,
        Func<IWriter, ValueTask> writing)
        where TConnection : DbConnection =>
        await connection.Execute(async () =>
        {
            await using var writer = await createWriter(connection);
            await writing(writer);
            await writer.Complete();
        });
    
    #endregion
    
    #region GetSustainableReader

    /// <summary>
    /// Возвращает устойчивого ридера
    /// </summary>
    public static IReader GetSustainableReader<TConnection>(this TConnection connection,
        Func<TConnection, IRow?, IWriter, CancellationToken, Task> reading,
        SustainableOperationsOptions<TConnection>? options = null,
        bool ownsConnection = false)
        where TConnection : DbConnection
    {
        options ??= SustainableOperationsOptions<TConnection>.Default;

        return new SustainableRelationalReader<TConnection>(
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

    #region GetSustainableWriter

    /// <summary>
    /// Возвращает устойчивого райтера
    /// </summary>
    public static IWriter GetSustainableWriter<TConnection>(this TConnection connection,
        Func<TConnection, IWriter> factory,
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