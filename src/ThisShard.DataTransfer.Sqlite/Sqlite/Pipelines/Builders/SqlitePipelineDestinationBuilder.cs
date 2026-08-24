using Microsoft.Data.Sqlite;
using ThisShard.Database.Core.Pipelines;
using ThisShard.Database.Core.Pipelines.Builders;
using ThisShard.Database.Core.Writers;
using ThisShard.Database.Infrastructure.Extensions;
using ThisShard.Database.Infrastructure.Sqlite.Models;
using ThisShard.Database.Infrastructure.Sqlite.Options;

namespace ThisShard.Database.Infrastructure.Sqlite.Pipelines.Builders;

/// <summary>
/// Билдер назначения Postgres
/// </summary>
public class SqlitePipelineDestinationBuilder : BasePipelineDestinationBuilder
{
    private string? _tableName;
    private SqliteTable? _table;
    private SqliteBulkOperationsOptions? _options;
    private Func<ValueTask<SqliteConnection>>? _connectionFactory;
    private bool _ownsConnection;
    
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
    /// Указать фабрику соединений
    /// </summary>
    public SqlitePipelineDestinationBuilder WithConnectionFactory(Func<ValueTask<SqliteConnection>> factory, bool ownsConnection = true)
    {
        _connectionFactory = factory;
        _ownsConnection = ownsConnection;
        return this;
    }

    /// <summary>
    /// Билдит назначение
    /// </summary>
    public override IPipelineDestination Build()
    {
        if (_connectionFactory == null)
            throw new InvalidOperationException("Connection factory must be set");

        var writerFactory = BuildWriterFactory();
        if (writerFactory == null)
            throw new InvalidOperationException("Either table path or table must be set");

        return new PipelineDestination<SqliteConnection>(
            Key,
            _connectionFactory,
            writerFactory,
            null,
            _ownsConnection
        );
    }

    /// <summary>
    /// Билдит фабрику писателей
    /// </summary>
    private Func<SqliteConnection, ValueTask<IRowWriter>>? BuildWriterFactory()
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