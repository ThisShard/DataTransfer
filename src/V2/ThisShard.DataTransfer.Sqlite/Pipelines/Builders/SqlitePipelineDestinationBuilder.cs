using Microsoft.Data.Sqlite;
using ThisShard.Database.Extensions;
using ThisShard.Database.Models;
using ThisShard.Database.Models.Tables;
using ThisShard.Database.Options;
using ThisShard.Database.Pipelines.Destinations;
using ThisShard.Database.Writers;

namespace ThisShard.Database.Pipelines.Builders;

/// <summary>
/// Билдер назначения Sqlite
/// </summary>
public class SqlitePipelineDestinationBuilder : IPipelineDestinationBuilder
{
    private string? _tableName;
    private SqliteTable? _table;
    private SqliteBulkOperationsOptions? _options;
    private Func<ValueTask<SqliteConnection>>? _connectionFactory;
    private bool _ownsConnection;
    private bool _createTableIfNotExists = true;

    /// <summary>
    /// Ключ
    /// </summary>
    public string Key { get; private set; } = string.Empty;

    /// <summary>
    /// Указать ключ для назначения
    /// </summary>
    IPipelineDestinationBuilder IPipelineDestinationBuilder.WithKey(string key) => WithKey(key);
    
    /// <summary>
    /// Указать ключ для назначения
    /// </summary>
    public SqlitePipelineDestinationBuilder WithKey(string key)
    {
        Key = key;
        return this;
    }
    
    /// <summary>
    /// Указать имя таблицы
    /// </summary>
    public SqlitePipelineDestinationBuilder WithTable(string name)
    {
        _tableName = name;
        return this;
    }
    
    /// <summary>
    /// Указать таблицу
    /// </summary>
    public SqlitePipelineDestinationBuilder WithTable(SqliteTable table)
    {
        _table = table;
        return this;
    }

    /// <summary>
    /// Указать настройки
    /// </summary>
    public SqlitePipelineDestinationBuilder WithOptions(SqliteBulkOperationsOptions options)
    {
        _options = options;
        return this;
    }

    /// <summary>
    /// Указать соединение
    /// </summary>
    public SqlitePipelineDestinationBuilder WithConnection(SqliteConnection connection, bool ownsConnection = false)
    {
        _connectionFactory = () => new ValueTask<SqliteConnection>(connection);
        _ownsConnection = ownsConnection;
        return this;
    }

    /// <summary>
    /// Указать фабрику соединений
    /// </summary>
    public SqlitePipelineDestinationBuilder WithConnectionFactory(Func<ValueTask<SqliteConnection>> factory, bool ownsConnection = true)
    {
        _connectionFactory = factory;
        _ownsConnection = ownsConnection;
        return this;
    }
    
    /// <summary>
    /// Указать признак создания таблиц
    /// </summary>
    public SqlitePipelineDestinationBuilder CreateTableIfNotExists(bool value = true)
    {
        _createTableIfNotExists = value;
        return this;
    }

    /// <summary>
    /// Билдит назначение
    /// </summary>
    public IPipelineDestination Build()
    {
        var connectionFactory = _connectionFactory;
        if (connectionFactory == null)
            throw new InvalidOperationException("Connection factory must be set");

        var writerFactory = BuildWriterFactory();
        if (writerFactory == null)
            throw new InvalidOperationException("Either table path or table must be set");
        
        var initFunc = BuildInitFunc();

        return new DbPipelineDestination<SqliteConnection>(
            Key,
            connectionFactory,
            writerFactory,
            initFunc,
            _ownsConnection
        );
    }

    /// <summary>
    /// Билдит функцию инициализации
    /// </summary>
    private Func<SqliteConnection, ITable?, ValueTask>? BuildInitFunc()
    {
        var tableName = _tableName;
        var options = _options;

        if (tableName != null && _createTableIfNotExists)
            return async (cn, table) =>
            {
                if (table == null)
                    return;
                
                await cn.CreateTable(table, tableName, options);
            };

        return null;
    }

    /// <summary>
    /// Билдит фабрику писателей
    /// </summary>
    private Func<SqliteConnection, ValueTask<IWriter>>? BuildWriterFactory()
    {
        var tableName = _tableName;
        var table = _table;
        var options = _options;
        
        if (tableName != null)
            return async cn => await cn.GetWriter(tableName, options);
        
        if (table != null)
            return async cn => await cn.GetWriter(table, options);
        
        return null;
    }
}