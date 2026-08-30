using ThisShard.Database.Core.Models.States;

namespace ThisShard.Database.Core.Pipelines.Builders;

/// <summary>
/// Билдер составного пайплайна
/// </summary>
public interface ICompositePipelineBuilder
{
    /// <summary>
    /// Ключ
    /// </summary>
    string Key { get; }
    
    /// <summary>
    /// Указать ключ для пайплайна
    /// </summary>
    ICompositePipelineBuilder WithKey(string key);
    
    /// <summary>
    /// Добавляет пайплайн
    /// </summary>
    ICompositePipelineBuilder AddPipeline(IPipelineBuilder pipeline);
    
    /// <summary>
    /// Билдит пайплайн
    /// </summary>
    IPipeline<CompositePipelineState> Build();
}