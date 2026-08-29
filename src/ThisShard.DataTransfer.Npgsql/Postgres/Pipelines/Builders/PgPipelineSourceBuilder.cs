using Npgsql;
using ThisShard.Database.Core.Extensions;
using ThisShard.Database.Core.Models.Rows;
using ThisShard.Database.Core.Models.Tables;
using ThisShard.Database.Core.Pipelines;
using ThisShard.Database.Core.Pipelines.Builders;
using ThisShard.Database.Core.Pipelines.Sources;
using ThisShard.Database.Core.Readers;
using ThisShard.Database.Infrastructure.Extensions;
using ThisShard.Database.Infrastructure.Postgres.Models;
using ThisShard.Database.Infrastructure.Postgres.Options;

namespace ThisShard.Database.Infrastructure.Postgres.Pipelines.Builders;

/// <summary>
/// Билдер источника Postgres
/// </summary>
public class PgPipelineSourceBuilder : IPipelineSourceBuilder
{
    private string[]? _tablePath;
    private PgTable? _table;
    private RowState _rowState = RowState.Added;
    private NpgsqlBulkOperationsOptions? _options;
    private Func<ValueTask<NpgsqlConnection>>? _connectionFactory;
    private Func<NpgsqlConnection, NpgsqlCommand>? _commandFactory;
    private bool _ownsConnection;

    /// <summary>
    /// Ключ
    /// </summary>
    public string Key { get; private set; } = string.Empty;

    /// <summary>
    /// Указать ключ для источника
    /// </summary>
    IPipelineSourceBuilder IPipelineSourceBuilder.WithKey(string key) => WithKey(key);

    /// <summary>
    /// Указать ключ для источника
    /// </summary>
    public PgPipelineSourceBuilder WithKey(string key)
    {
        Key = key;
        return this;
    }

    /// <summary>
    /// Указать путь к таблице
    /// </summary>
    public PgPipelineSourceBuilder WithTable(string[] path)
    {
        _tablePath = path;
        return this;
    }
    
    /// <summary>
    /// Указать таблицу
    /// </summary>
    public PgPipelineSourceBuilder WithTable(PgTable table)
    {
        _table = table;
        return this;
    }

    /// <summary>
    /// Указать RowState
    /// </summary>
    public PgPipelineSourceBuilder WithRowState(RowState rowState)
    {
        _rowState = rowState;
        return this;
    }

    /// <summary>
    /// Указать настройки
    /// </summary>
    public PgPipelineSourceBuilder WithOptions(NpgsqlBulkOperationsOptions options)
    {
        _options = options;
        return this;
    }

    /// <summary>
    /// Указать фабрику соединений
    /// </summary>
    public PgPipelineSourceBuilder WithConnectionFactory(Func<ValueTask<NpgsqlConnection>> factory, bool ownsConnection = true)
    {
        _connectionFactory = factory;
        _ownsConnection = ownsConnection;
        return this;
    }
    
    /// <summary>
    /// Указать фабрику команд
    /// </summary>
    public PgPipelineSourceBuilder WithCommandFactory(Func<NpgsqlConnection, NpgsqlCommand> factory)
    {
        _commandFactory = factory;
        return this;
    }
    
    /// <summary>
    /// Билдит источник
    /// </summary>
    public IPipelineSource Build()
    {
        var connectionFactory = _connectionFactory;
        if (connectionFactory == null)
            throw new InvalidOperationException("Connection factory must be set");

        var readerFactory = BuildReaderFactory();
        if (readerFactory == null)
            throw new InvalidOperationException("Either table path or table must be set");

        var tableGetter = BuildTableGetter();

        return new DbPipelineSource<NpgsqlConnection>(
            Key,
            connectionFactory,
            readerFactory,
            tableGetter,
            _ownsConnection
        );
    }

    /// <summary>
    /// Билдит геттер таблиц
    /// </summary>
    private Func<NpgsqlConnection, ValueTask<ITable?>>? BuildTableGetter()
    {
        var tablePath = _tablePath;
        var table = _table;
        var options = _options;
        
        if (tablePath != null)
            return async cn => await cn.GetTableInfo(tablePath, options);
        
        if (table != null)
            return _ => ValueTask.FromResult<ITable>(table)!;

        return null;
    }

    /// <summary>
    /// Билдит фабрику ридеров
    /// </summary>
    private Func<NpgsqlConnection, IRow?, ValueTask<IRowReader>>? BuildReaderFactory()
    {
        var tablePath = _tablePath;
        var table = _table;
        var commandFactory = _commandFactory;
        var rowState = _rowState;
        var options = _options;
        
        if (tablePath != null)
        {
            if (commandFactory != null)
                return (cn, row) =>
                    cn.GetSustainableRowReader(tablePath,
                        commandFactory,
                        rowState,
                        options,
                        false,
                        row);
            
            return (cn, row) =>
                cn.GetSustainableRowReader(tablePath,
                    rowState,
                    options,
                    false,
                    row);
        }
        
        if (table != null)
        {
            if (commandFactory != null)
                return (cn, row) => 
                        ValueTask.FromResult(cn.GetSustainableRowReader(table,
                            commandFactory,
                            rowState,
                            options,
                            false,
                            row));
            
            return (cn, row) =>
                ValueTask.FromResult(cn.GetSustainableRowReader(table,
                    rowState,
                    options,
                    false,
                    row));
        }

        return null;
    }
}