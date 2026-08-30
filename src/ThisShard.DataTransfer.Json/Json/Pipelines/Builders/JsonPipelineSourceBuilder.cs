using System.Text.Json.Stream;
using ThisShard.Database.Core.Models.Rows;
using ThisShard.Database.Core.Pipelines.Builders;
using ThisShard.Database.Core.Pipelines.Sources;
using ThisShard.Database.Core.Readers;
using ThisShard.Database.Infrastructure.Extensions;

namespace ThisShard.Database.Infrastructure.Json.Pipelines.Builders;

/// <summary>
/// Билдер источника Json
/// </summary>
public class JsonPipelineSourceBuilder : IPipelineSourceBuilder
{
    private RowState _rowState = RowState.AddedOrModified;
    private Func<ValueTask<IUtf8JsonAsyncStreamReader>>? _readerFactory;
    private Func<string, string>? _propertyNameResolver;
    private string? _rowStatePropertyName;
    private bool _ownsReader;
    
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
    public JsonPipelineSourceBuilder WithKey(string key)
    {
        Key = key;
        return this;
    }
    
    /// <summary>
    /// Указать RowState
    /// </summary>
    public JsonPipelineSourceBuilder WithRowState(RowState rowState)
    {
        _rowState = rowState;
        return this;
    }

    /// <summary>
    /// Указать имя свойства для RowState
    /// </summary>
    public JsonPipelineSourceBuilder WithRowStatePropertyName(string rowStatePropertyName)
    {
        _rowStatePropertyName = rowStatePropertyName;
        return this;
    }

    /// <summary>
    /// Указать резолвер имен свойств
    /// </summary>
    public JsonPipelineSourceBuilder WithPropertyNameResolver(Func<string, string> propertyNameResolver)
    {
        _propertyNameResolver = propertyNameResolver;
        return this;
    }

    /// <summary>
    /// Указать ридер
    /// </summary>
    public JsonPipelineSourceBuilder WithReader(IUtf8JsonAsyncStreamReader reader, bool ownsReader = false)
    {
        _readerFactory = () => new ValueTask<IUtf8JsonAsyncStreamReader>(reader);
        _ownsReader = ownsReader;
        return this;
    }

    /// <summary>
    /// Указать фабрику ридеров
    /// </summary>
    public JsonPipelineSourceBuilder WithReaderFactory(Func<ValueTask<IUtf8JsonAsyncStreamReader>> factory, bool ownsReader = true)
    {
        _readerFactory = factory;
        _ownsReader = ownsReader;
        return this;
    }
    
    /// <summary>
    /// Билдит источник
    /// </summary>
    public IPipelineSource Build()
    {
        var connectionFactory = _readerFactory;
        if (connectionFactory == null)
            throw new InvalidOperationException("Reader factory must be set");
        
        var readerFactory = BuildReaderFactory();

        return new PipelineSource<IUtf8JsonAsyncStreamReader>(
            Key,
            connectionFactory,
            readerFactory,
            null,
            _ownsReader
        );
    }

    /// <summary>
    /// Билдит фабрику ридеров
    /// </summary>
    private Func<IUtf8JsonAsyncStreamReader, IRow?, ValueTask<IRowReader>> BuildReaderFactory()
    {
        var rowState = _rowState;
        var propertyNameResolver = _propertyNameResolver;
        var rowStatePropertyName = _rowStatePropertyName;

        return (cn, _) =>
            ValueTask.FromResult<IRowReader>(cn.GetRowReader(rowState, propertyNameResolver, rowStatePropertyName,
                false));
    }
}