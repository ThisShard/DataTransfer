using ThisShard.Database.Core.Models.States;

namespace ThisShard.Database.Core.Pipelines.Builders;

/// <summary>
/// Билдер пайплайна
/// </summary>
public interface IPipelineBuilder
{
    /// <summary>
    /// Ключ
    /// </summary>
    string Key { get; }

    /// <summary>
    /// Указать ключ для пайплайна
    /// </summary>
    IPipelineBuilder WithKey(string key);
    
    /// <summary>
    /// Добавляет источник
    /// </summary>
    IPipelineBuilder AddSource(IPipelineSourceBuilder source);
    
    /// <summary>
    /// Добавляет назначение
    /// </summary>
    IPipelineBuilder AddDestination(IPipelineDestinationBuilder destination);
    
    /// <summary>
    /// Билдит пайплайн
    /// </summary>
    IPipeline<PipelineState> Build();
}