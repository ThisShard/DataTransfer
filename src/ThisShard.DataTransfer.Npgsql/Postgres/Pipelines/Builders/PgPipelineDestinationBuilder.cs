using Npgsql;
using ThisShard.Database.Core.Pipelines;
using ThisShard.Database.Core.Pipelines.Builders;
using ThisShard.Database.Core.Writers;
using ThisShard.Database.Infrastructure.Extensions;
using ThisShard.Database.Infrastructure.Postgres.Models;
using ThisShard.Database.Infrastructure.Postgres.Options;

namespace ThisShard.Database.Infrastructure.Postgres.Pipelines.Builders;

/// <summary>
/// Билдер назначения Postgres
/// </summary>
public class PgPipelineDestinationBuilder : BasePipelineDestinationBuilder
{
    private string[]? _tablePath;
    private PgStagingTable? _stagingTable;
    private PgTable? _table;
    private NpgsqlBulkOperationsOptions? _options;
    private Func<ValueTask<NpgsqlConnection>>? _connectionFactory;
    private bool _ownsConnection;
    private bool _useBulkWriter = true;
    
    /// <summary>
    /// Указать путь к таблице
    /// </summary>
    public PgPipelineDestinationBuilder WithTable(string[] path)
    {
        _tablePath = path;
        return this;
    }
    
    /// <summary>
    /// Указать таблицу
    /// </summary>
    public PgPipelineDestinationBuilder WithTable(PgTable table)
    {
        _table = table;
        return this;
    }
    
    /// <summary>
    /// Указать временную таблицу
    /// </summary>
    public PgPipelineDestinationBuilder WithStagingTable(PgStagingTable table)
    {
        _stagingTable = table;
        return this;
    }

    /// <summary>
    /// Указать настройки
    /// </summary>
    public PgPipelineDestinationBuilder WithOptions(NpgsqlBulkOperationsOptions options)
    {
        _options = options;
        return this;
    }

    /// <summary>
    /// Указать фабрику соединений
    /// </summary>
    public PgPipelineDestinationBuilder WithConnectionFactory(Func<ValueTask<NpgsqlConnection>> factory, bool ownsConnection = true)
    {
        _connectionFactory = factory;
        _ownsConnection = ownsConnection;
        return this;
    }

    /// <summary>
    /// Использовать Bulk писатель
    /// </summary>
    public PgPipelineDestinationBuilder WithBulkWriter(bool value = true)
    {
        _useBulkWriter = value;
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
            throw new InvalidOperationException("Either table path or table or staging table must be set");

        return new PipelineDestination<NpgsqlConnection>(
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
    private Func<NpgsqlConnection, ValueTask<IRowWriter>>? BuildWriterFactory()
    {
        var tablePath = _tablePath;
        var table = _table;
        var stagingTable = _stagingTable;
        var options = _options;
        
        if (tablePath != null)
        {
            if (_useBulkWriter)
                return async cn => await cn.GetBulkWriter(tablePath, options);
            
            return async cn => await cn.GetBatchWriter(tablePath, options);
        }
        
        if (table != null)
        {
            if (_useBulkWriter)
                return async cn => await cn.GetBulkWriter(table, options);
            
            return async cn => await cn.GetBatchWriter(table, options);
        }
        
        if (stagingTable != null)
        {
            if (_useBulkWriter)
                return async cn => await cn.GetBulkWriter(stagingTable, options);
            
            return async cn => await cn.GetBatchWriter(stagingTable, options);
        }

        return null;
    }
}