using ThisShard.Database.Core.Pipelines.Destinations;

namespace ThisShard.Database.Core.Pipelines.Builders;

/// <summary>
/// Билдер назначения
/// </summary>
public interface IPipelineDestinationBuilder
{
    /// <summary>
    /// Ключ
    /// </summary>
    string Key { get; }
    
    /// <summary>
    /// Указать ключ для назначения
    /// </summary>
    IPipelineDestinationBuilder WithKey(string key);
    
    /// <summary>
    /// Билдит назначение
    /// </summary>
    IPipelineDestination Build();
}