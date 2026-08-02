namespace ThisShard.Database.Core.Pipelines.Builders;

/// <summary>
/// Билдер пайплайна
/// </summary>
public interface IPipelineBuilder
{
    /// <summary>
    /// Добавляет источник
    /// </summary>
    public IPipelineBuilder AddSource(IPipelineSourceBuilder sourceBuilder);
    
    /// <summary>
    /// Добавляет назначение
    /// </summary>
    public IPipelineBuilder AddDestination(IPipelineDestinationBuilder destinationBuilder);
}