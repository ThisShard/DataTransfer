using Microsoft.Data.Sqlite;
using ThisShard.Database.Core.Extensions;
using ThisShard.Database.Core.Models.Rows;
using ThisShard.Database.Core.Models.Tables;
using ThisShard.Database.Core.Pipelines;
using ThisShard.Database.Core.Pipelines.Builders;
using ThisShard.Database.Core.Readers;
using ThisShard.Database.Infrastructure.Extensions;
using ThisShard.Database.Infrastructure.Sqlite.Models;
using ThisShard.Database.Infrastructure.Sqlite.Options;

namespace ThisShard.Database.Infrastructure.Sqlite.Pipelines.Builders;

/// <summary>
/// Билдер источника Sqlite
/// </summary>
public class SqlitePipelineSourceBuilder : IPipelineSourceBuilder
{
    private string? _tableName;
    private SqliteTable? _table;
    private RowState _rowState = RowState.Added;
    private SqliteBulkOperationsOptions? _options;
    private Func<ValueTask<SqliteConnection>>? _connectionFactory;
    private Func<SqliteConnection, SqliteCommand>? _commandFactory;
    private bool _ownsConnection;
    private string _key = string.Empty;
    
    /// <summary>
    /// Указать ключ для источника
    /// </summary>
    public IPipelineSourceBuilder WithKey(string key)
    {
        _key = key;
        return this;
    }

    /// <summary>
    /// Указать имя таблицы
    /// </summary>
    public SqlitePipelineSourceBuilder WithTable(string name)
    {
        _tableName = name;
        return this;
    }
    
    /// <summary>
    /// Указать таблицу
    /// </summary>
    public SqlitePipelineSourceBuilder WithTable(SqliteTable table)
    {
        _table = table;
        return this;
    }

    /// <summary>
    /// Указать RowState
    /// </summary>
    public SqlitePipelineSourceBuilder WithRowState(RowState rowState)
    {
        _rowState = rowState;
        return this;
    }

    /// <summary>
    /// Указать настройки
    /// </summary>
    public SqlitePipelineSourceBuilder WithOptions(SqliteBulkOperationsOptions options)
    {
        _options = options;
        return this;
    }

    /// <summary>
    /// Указать фабрику соединений
    /// </summary>
    public SqlitePipelineSourceBuilder WithConnectionFactory(Func<ValueTask<SqliteConnection>> factory, bool ownsConnection = true)
    {
        _connectionFactory = factory;
        _ownsConnection = ownsConnection;
        return this;
    }
    
    /// <summary>
    /// Указать фабрику команд
    /// </summary>
    public SqlitePipelineSourceBuilder WithCommandFactory(Func<SqliteConnection, SqliteCommand> factory)
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

        return new PipelineSource<SqliteConnection>(
            _key,
            async () =>
            {
                var connection = await connectionFactory();
                
                if (_ownsConnection && !connection.IsOpen())
                    await connection.OpenAsync();
                
                return connection;
            },
            readerFactory,
            tableGetter,
            _ownsConnection
        );
    }

    /// <summary>
    /// Билдит геттер таблиц
    /// </summary>
    private Func<SqliteConnection, ValueTask<ITable?>>? BuildTableGetter()
    {
        var tableName = _tableName;
        var table = _table;
        var options = _options;
        
        if (tableName != null)
            return async cn => await cn.GetTableInfo(tableName, options);
        
        if (table != null)
            return _ => ValueTask.FromResult<ITable>(table)!;

        return null;
    }

    /// <summary>
    /// Билдит фабрику ридеров
    /// </summary>
    private Func<SqliteConnection, IRow?, ValueTask<IRowReader>>? BuildReaderFactory()
    {
        var tableName = _tableName;
        var table = _table;
        var commandFactory = _commandFactory;
        var rowState = _rowState;
        var options = _options;
        
        if (tableName != null)
        {
            if (commandFactory != null)
                return (cn, row) =>
                    cn.GetSustainableRowReader(tableName,
                        commandFactory,
                        rowState,
                        options,
                        false,
                        row);
            
            return (cn, row) =>
                cn.GetSustainableRowReader(tableName,
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