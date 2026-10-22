using System.Data.Common;
using ThisShard.Database.Extensions;
using ThisShard.Database.Models.Rows;
using ThisShard.Database.Models.Tables;
using ThisShard.Database.Readers;

namespace ThisShard.Database.Pipelines.Sources;

/// <summary>
/// Источник данных для конвейера с DbConnection
/// </summary>
public class DbPipelineSource<TConnection> : PipelineSource<TConnection>
    where TConnection : DbConnection
{
    private bool _isConnectionOpen;
    
    public DbPipelineSource(
        string key, 
        Func<ValueTask<TConnection>> connectionFactory, 
        Func<TConnection, IRow?, ValueTask<IReader>> readerFactory, 
        Func<TConnection, ValueTask<ITable?>>? tableGetter = null, 
        bool ownsConnection = true
        ) : base(key, connectionFactory, readerFactory, tableGetter, ownsConnection)
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