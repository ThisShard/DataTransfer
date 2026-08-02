using ThisShard.Database.Core.Mappers.Middlewares;
using ThisShard.Database.Core.Models;
using ThisShard.Database.Core.Models.Rows;

namespace ThisShard.Database.Core.Mappers.Builders;

/// <summary>
/// Билдер маппера строк
/// </summary>
public class RowMapperBuilder
{
    private readonly List<IValueMappingMiddleware> _middlewares = new();
    private Func<IRow, RowState> _getRowState = row => row.State;

    /// <summary>
    /// Добавить миддлварь
    /// </summary>
    public RowMapperBuilder AddMiddleware(IValueMappingMiddleware middleware)
    {
        _middlewares.Add(middleware);
        return this;
    }

    /// <summary>
    /// Использовать конвертер состояний строк
    /// </summary>
    public RowMapperBuilder UseRowStateConverter(Func<IRow, RowState> getRowState)
    {
        _getRowState = getRowState;
        return this;
    }
    
    /// <summary>
    /// Билдит маппер
    /// </summary>
    /// <returns></returns>
    public IRowMapper Build()
    {
        return new RowMapper(_middlewares.ToArray(), _getRowState);
    }

    #region Static

    /// <summary>
    /// Создает новый билдер
    /// </summary>
    public static RowMapperBuilder Create() => new();

    #endregion
}