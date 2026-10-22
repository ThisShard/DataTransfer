using ThisShard.Database.Pipelines.Sources;

namespace ThisShard.Database.Pipelines.Builders;

/// <summary>
/// Билдер источника
/// </summary>
public interface IPipelineSourceBuilder
{
    /// <summary>
    /// Ключ
    /// </summary>
    string Key { get; }
    
    /// <summary>
    /// Указать ключ для источника
    /// </summary>
    IPipelineSourceBuilder WithKey(string key);
    
    /// <summary>
    /// Билдит источник
    /// </summary>
    IPipelineSource Build();
}