namespace ThisShard.Database.Core.Pipelines.Builders;

/// <summary>
/// Билдер пайплайна
/// </summary>
public class PipelineBuilder : IPipelineBuilder
{
    private readonly List<IPipelineSourceBuilder> _sourceBuilders = new();
    private readonly List<IPipelineDestinationBuilder> _destinationBuilders = new();
    
    private string _key = string.Empty;

    /// <summary>
    /// Указать ключ для пайплайна
    /// </summary>
    public IPipelineBuilder WithKey(string key)
    {
        _key = key;
        return this;
    }
    
    /// <summary>
    /// Добавляет источник
    /// </summary>
    public IPipelineBuilder AddSource(IPipelineSourceBuilder source)
    {
        _sourceBuilders.Add(source);
        return this;
    }


    /// <summary>
    /// Добавляет назначение
    /// </summary>
    public IPipelineBuilder AddDestination(IPipelineDestinationBuilder destination)
    {
        _destinationBuilders.Add(destination);
        return this;
    }

    /// <summary>
    /// Билдит пайплайн
    /// </summary>
    public IPipeline Build()
    {
        var sources = _sourceBuilders.Select(x => x.Build()).ToArray();
        var destinations = _destinationBuilders.Select(x => x.Build()).ToArray();
        
        return new Pipeline(_key, sources, destinations);
    }
}