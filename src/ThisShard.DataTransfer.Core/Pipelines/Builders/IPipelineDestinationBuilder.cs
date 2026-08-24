namespace ThisShard.Database.Core.Pipelines.Builders;

/// <summary>
/// Билдер назначения
/// </summary>
public interface IPipelineDestinationBuilder
{
    /// <summary>
    /// Указать ключ для назначения
    /// </summary>
    IPipelineDestinationBuilder WithKey(string key);
    
    /// <summary>
    /// Билдит назначение
    /// </summary>
    IPipelineDestination Build();
}