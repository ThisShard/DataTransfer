using ThisShard.Database.Mappers.Middlewares;

namespace ThisShard.Database.Mappers.Builders;

/// <summary>
/// Билдер маппера строк
/// </summary>
public class RowMapperBuilder
{
    private readonly List<IRowMappingMiddleware> _middlewares = new();

    /// <summary>
    /// Добавить миддлварь
    /// </summary>
    public RowMapperBuilder AddMiddleware(IRowMappingMiddleware middleware)
    {
        _middlewares.Add(middleware);
        return this;
    }
    
    /// <summary>
    /// Билдит маппер
    /// </summary>
    /// <returns></returns>
    public IRowMapper Build()
    {
        return new RowMapper(_middlewares);
    }

    #region Static

    /// <summary>
    /// Создает новый билдер
    /// </summary>
    public static RowMapperBuilder Create() => new();

    #endregion
}