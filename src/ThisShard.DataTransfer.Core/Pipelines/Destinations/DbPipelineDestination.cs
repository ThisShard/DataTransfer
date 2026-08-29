using System.Data.Common;
using ThisShard.Database.Core.Extensions;
using ThisShard.Database.Core.Models.Tables;
using ThisShard.Database.Core.Writers;

namespace ThisShard.Database.Core.Pipelines.Destinations;

/// <summary>
/// Назначение данных для конвейера с DbConnection
/// </summary>
public class DbPipelineDestination<TConnection> : PipelineDestination<TConnection>
    where TConnection : DbConnection
{
    private bool _isConnectionOpen;

    public DbPipelineDestination(
        string key, 
        Func<ValueTask<TConnection>> connectionFactory, 
        Func<TConnection, ValueTask<IRowWriter>> writerFactory, 
        Func<TConnection, ITable, ValueTask>? initFunc = null, 
        bool ownsConnection = true
        ) : base(key, connectionFactory, writerFactory, initFunc, ownsConnection)
    {
    }

    /// <summary>
    /// Создает соединение
    /// </summary>
    protected override async ValueTask<TConnection> CreateConnection()
    {
        var connection = await base.CreateConnection();

        _isConnectionOpen = connection.IsOpen();
        if (!_isConnectionOpen)
            await connection.OpenAsync();
        
        return connection;
    }

    /// <summary>
    /// Закрывает соединение
    /// </summary>
    protected override async ValueTask CloseConnection()
    {
        if (!_isConnectionOpen && Connection!.IsOpen())
            await Connection!.CloseAsync();
    }
}