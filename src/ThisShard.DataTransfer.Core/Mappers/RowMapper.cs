using ThisShard.Database.Core.Mappers.Middlewares;
using ThisShard.Database.Core.Models;
using ThisShard.Database.Core.Models.Rows;

namespace ThisShard.Database.Core.Mappers;

/// <summary>
/// Маппер значений строк
/// </summary>
public class RowMapper : IRowMapper
{
    private readonly IValueMappingMiddleware[] _middlewares;
    private readonly Func<IRow, RowState> _getRowState;

    public RowMapper(IValueMappingMiddleware[] middlewares, Func<IRow, RowState> getRowState)
    {
        _middlewares = middlewares;
        _getRowState = getRowState;
    }

    /// <summary>
    /// Возвращает смапленное состояние строки
    /// </summary>
    public RowState GetRowState(IRow source) => _getRowState(source);

    /// <summary>
    /// Пытается получить смапленное значение ячейки у строки
    /// </summary>
    public bool TryGetValue(IRow source, string columnKey, out object? value) => 
        TryGetValueChain(source, columnKey, out value, 0);

    /// <summary>
    /// Выполняет по цепочке все миддлвари по получению смапленного состояния значения ячейки у строки
    /// </summary>
    private bool TryGetValueChain(IRow source, string columnKey, out object? value, int index)
    {
        if (index >= _middlewares.Length)
            return source.TryGetValue(columnKey, out value);

        bool NextDelegate(IRow s, string k, out object? v) => TryGetValueChain(s, k, out v, index + 1);

        return _middlewares[index].TryGetValue(source, columnKey, out value, NextDelegate);
    }
}