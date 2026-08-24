using ThisShard.Database.Core.Models.States;

namespace ThisShard.Database.Core.Pipelines.Builders;

/// <summary>
/// Билдер составного пайплайна
/// </summary>
public class CompositePipelineBuilder : ICompositePipelineBuilder
{
    private readonly List<IPipelineBuilder> _builders = new();
    
    private string _key = string.Empty;

    /// <summary>
    /// Указать ключ для пайплайна
    /// </summary>
    public ICompositePipelineBuilder WithKey(string key)
    {
        _key = key;
        return this;
    }
    
    /// <summary>
    /// Добавляет пайплайн
    /// </summary>
    public ICompositePipelineBuilder AddPipeline(IPipelineBuilder pipeline)
    {
        _builders.Add(pipeline);
        return this;
    }

    /// <summary>
    /// Билдит пайплайн
    /// </summary>
    public IPipeline<CompositePipelineState> Build()
    {
        return new CompositePipeline(_key, _builders.Select(x=>x.Build()));
    }
}