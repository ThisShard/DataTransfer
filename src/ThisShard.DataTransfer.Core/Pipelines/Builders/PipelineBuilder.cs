using ThisShard.Database.Core.Models.States;

namespace ThisShard.Database.Core.Pipelines.Builders;

/// <summary>
/// Билдер пайплайна
/// </summary>
public class PipelineBuilder : IPipelineBuilder
{
    private readonly List<IPipelineSourceBuilder> _sourceBuilders = new();
    private readonly List<IPipelineDestinationBuilder> _destinationBuilders = new();
    private int _nextSourceDefaultKeyIndex = 0;
    private int _nextDestinationDefaultKeyIndex = 0;
    
    /// <summary>
    /// Ключ
    /// </summary>
    public string Key { get; private set; } = string.Empty;

    /// <summary>
    /// Указать ключ для пайплайна
    /// </summary>
    public IPipelineBuilder WithKey(string key)
    {
        Key = key;
        return this;
    }
    
    /// <summary>
    /// Добавляет источник
    /// </summary>
    public IPipelineBuilder AddSource(IPipelineSourceBuilder source)
    {
        if (string.IsNullOrEmpty(source.Key))
            source.WithKey($"Source_{_nextSourceDefaultKeyIndex++}");
        
        _sourceBuilders.Add(source);
        return this;
    }


    /// <summary>
    /// Добавляет назначение
    /// </summary>
    public IPipelineBuilder AddDestination(IPipelineDestinationBuilder destination)
    {
        if (string.IsNullOrEmpty(destination.Key))
            destination.WithKey($"Destination_{_nextSourceDefaultKeyIndex++}");
        
        _destinationBuilders.Add(destination);
        return this;
    }

    /// <summary>
    /// Билдит пайплайн
    /// </summary>
    public IPipeline<PipelineState> Build()
    {
        var sources = _sourceBuilders.Select(x => x.Build()).ToArray();
        var destinations = _destinationBuilders.Select(x => x.Build()).ToArray();
        
        return new Pipeline(Key, sources, destinations);
    }
}