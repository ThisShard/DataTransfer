using System.Text.Json;
using ThisShard.Database.Core.Models.Tables;
using ThisShard.Database.Core.Pipelines.Builders;
using ThisShard.Database.Core.Pipelines.Destinations;
using ThisShard.Database.Core.Writers;
using ThisShard.Database.Infrastructure.Extensions;
using ThisShard.Database.Infrastructure.Json.Models;
using ThisShard.Database.Infrastructure.Json.Options;

namespace ThisShard.Database.Infrastructure.Json.Pipelines.Builders;

public class JsonPipelineDestinationBuilder : IPipelineDestinationBuilder
{
    private Type? _entityType;
    private ITable? _table;
    private JsonBulkOperationsOptions? _options;
    private Func<ValueTask<Utf8JsonWriter>>? _writerFactory;
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
    public JsonPipelineDestinationBuilder WithKey(string key)
    {
        Key = key;
        return this;
    }

    /// <summary>
    /// Указать тип сущности
    /// </summary>
    public JsonPipelineDestinationBuilder WithEntityType(Type entityType)
    {
        _entityType = entityType;
        return this;
    }
    
    /// <summary>
    /// Указать таблицу
    /// </summary>
    public JsonPipelineDestinationBuilder WithTable(ITable table)
    {
        _table = table;
        return this;
    }

    /// <summary>
    /// Указать настройки
    /// </summary>
    public JsonPipelineDestinationBuilder WithOptions(JsonBulkOperationsOptions options)
    {
        _options = options;
        return this;
    }

    /// <summary>
    /// Указать писателя
    /// </summary>
    public JsonPipelineDestinationBuilder WithWriter(Utf8JsonWriter writer, bool ownsWriter = false)
    {
        _writerFactory = () => new ValueTask<Utf8JsonWriter>(writer);
        _ownsWriter = ownsWriter;
        return this;
    }

    /// <summary>
    /// Указать фабрику писателей
    /// </summary>
    public JsonPipelineDestinationBuilder WithReaderFactory(Func<ValueTask<Utf8JsonWriter>> factory, bool ownsWriter = true)
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

        return new PipelineDestination<Utf8JsonWriter>(
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
    private Func<Utf8JsonWriter, ITable?, ValueTask<IRowWriter>> BuildWriterFactory()
    {
        var table = _table;
        var entityType = _entityType;
        var options = _options;

        if (table is JsonTable jsonTable)
            return async (cn, _) => await cn.GetTableWriter(jsonTable, options);
        
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