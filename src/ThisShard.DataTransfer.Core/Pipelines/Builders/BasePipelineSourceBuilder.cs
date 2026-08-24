namespace ThisShard.Database.Core.Pipelines.Builders;

/// <summary>
/// Базовый билдер источника
/// </summary>
public abstract class BasePipelineSourceBuilder : IPipelineSourceBuilder
{
    /// <summary>
    /// Ключ
    /// </summary>
    protected string Key { get; private set; } = string.Empty;

    /// <summary>
    /// Указать ключ для источника
    /// </summary>
    public IPipelineSourceBuilder WithKey(string key)
    {
        Key = key;
        return this;
    }

    /// <summary>
    /// Билдит источник
    /// </summary>
    public abstract IPipelineSource Build();
}