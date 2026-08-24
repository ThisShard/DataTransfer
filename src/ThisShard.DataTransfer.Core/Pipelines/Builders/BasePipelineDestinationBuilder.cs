namespace ThisShard.Database.Core.Pipelines.Builders;

/// <summary>
/// Базовый билдер назначения
/// </summary>
public abstract class BasePipelineDestinationBuilder : IPipelineDestinationBuilder
{
    /// <summary>
    /// Ключ
    /// </summary>
    protected string Key { get; private set; } = string.Empty;

    /// <summary>
    /// Указать ключ для назначения
    /// </summary>
    public IPipelineDestinationBuilder WithKey(string key)
    {
        Key = key;
        return this;
    }

    /// <summary>
    /// Билдит назначение
    /// </summary>
    public abstract IPipelineDestination Build();
}