using CsvHelper;
using ThisShard.Database.Core.Models.Tables;
using ThisShard.Database.Core.Pipelines.Builders;
using ThisShard.Database.Core.Pipelines.Destinations;
using ThisShard.Database.Core.Writers;
using ThisShard.Database.Infrastructure.Extensions;
using ThisShard.Database.Infrastructure.Csv.Models;
using ThisShard.Database.Infrastructure.Csv.Options;

namespace ThisShard.Database.Infrastructure.Csv.Pipelines.Builders;

public class CsvPipelineDestinationBuilder : IPipelineDestinationBuilder
{
    private Type? _entityType;
    private ITable? _table;
    private CsvBulkOperationsOptions? _options;
    private Func<ValueTask<CsvWriter>>? _writerFactory;
    private bool _ownsWriter;
    
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
    public CsvPipelineDestinationBuilder WithKey(string key)
    {
        Key = key;
        return this;
    }

    /// <summary>
    /// Указать тип сущности
    /// </summary>
    public CsvPipelineDestinationBuilder WithEntityType(Type entityType)
    {
        _entityType = entityType;
        return this;
    }
    
    /// <summary>
    /// Указать таблицу
    /// </summary>
    public CsvPipelineDestinationBuilder WithTable(ITable table)
    {
        _table = table;
        return this;
    }

    /// <summary>
    /// Указать настройки
    /// </summary>
    public CsvPipelineDestinationBuilder WithOptions(CsvBulkOperationsOptions options)
    {
        _options = options;
        return this;
    }

    /// <summary>
    /// Указать писателя
    /// </summary>
    public CsvPipelineDestinationBuilder WithWriter(CsvWriter writer, bool ownsWriter = false)
    {
        _writerFactory = () => new ValueTask<CsvWriter>(writer);
        _ownsWriter = ownsWriter;
        return this;
    }

    /// <summary>
    /// Указать фабрику писателей
    /// </summary>
    public CsvPipelineDestinationBuilder WithWriterFactory(Func<ValueTask<CsvWriter>> factory, bool ownsWriter = true)
    {
        _writerFactory = factory;
        _ownsWriter = ownsWriter;
        return this;
    }
    
    /// <summary>
    /// Билдит назначение
    /// </summary>
    public IPipelineDestination Build()
    {
        var connectionFactory = _writerFactory;
        if (connectionFactory == null)
            throw new InvalidOperationException("Writer factory must be set");

        var writerFactory = BuildWriterFactory();

        return new PipelineDestination<CsvWriter>(
            Key,
            connectionFactory,
            writerFactory,
            null,
            _ownsWriter
        );
    }
    
    /// <summary>
    /// Билдит фабрику писателей
    /// </summary>
    private Func<CsvWriter, ITable?, ValueTask<IRowWriter>> BuildWriterFactory()
    {
        var table = _table;
        var entityType = _entityType;
        var options = _options;

        if (table is CsvTable csvTable)
            return async (cn, _) => await cn.GetTableWriter(csvTable, options);
        
        if (table != null)
            return async (cn, _) => await cn.GetTableWriter(table, options);
        
        if (entityType != null)
            return async (cn, _) => await cn.GetTableWriter(entityType, options);

        return async (cn, t) =>
        {
            if (t == null)
                throw new InvalidOperationException("Table must be provided");
            
            return await cn.GetTableWriter(t, options);
        };
    }
}