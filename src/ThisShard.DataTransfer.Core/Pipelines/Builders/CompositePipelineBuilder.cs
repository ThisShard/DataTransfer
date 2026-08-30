using ThisShard.Database.Core.Models.States;

namespace ThisShard.Database.Core.Pipelines.Builders;

/// <summary>
/// Билдер составного пайплайна
/// </summary>
public class CompositePipelineBuilder : ICompositePipelineBuilder
{
    private readonly List<IPipelineBuilder> _builders = new();
    private int _nextDefaultKeyIndex = 0;
    private int _maxDop = 1;

    /// <summary>
    /// Ключ
    /// </summary>
    public string Key { get; private set; } = string.Empty;

    /// <summary>
    /// Указать ключ для пайплайна
    /// </summary>
    public ICompositePipelineBuilder WithKey(string key)
    {
        Key = key;
        return this;
    }

    /// <summary>
    /// С максимальным паралеллизмом
    /// </summary>
    public ICompositePipelineBuilder WithMaxDop(int maxDop)
    {
        _maxDop = maxDop;
        return this;
    }
    
    /// <summary>
    /// Добавляет пайплайн
    /// </summary>
    public ICompositePipelineBuilder AddPipeline(IPipelineBuilder pipeline)
    {
        if (string.IsNullOrEmpty(pipeline.Key))
            pipeline.WithKey($"Pipeline_{_nextDefaultKeyIndex++}");
        
        _builders.Add(pipeline);
        return this;
    }

    /// <summary>
    /// Билдит пайплайн
    /// </summary>
    public IPipeline<CompositePipelineState> Build()
    {
        return new CompositePipeline(Key, _builders.Select(x=>x.Build()), _maxDop);
    }
}