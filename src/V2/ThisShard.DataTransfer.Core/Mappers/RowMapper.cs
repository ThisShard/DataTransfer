using ThisShard.Database.Mappers.Middlewares;
using ThisShard.Database.Models.Rows;

namespace ThisShard.Database.Mappers;

/// <summary>
/// Маппер значений строк
/// </summary>
public class RowMapper : IRowMapper
{
    private readonly IValueMappingMiddleware[] _valueMiddlewares;
    private readonly IRowStateMappingMiddleware[] _rowStateMiddlewares;
    private readonly IKeysMappingMiddleware[] _keysMiddlewares;

    public RowMapper(IEnumerable<IRowMappingMiddleware> middlewares)
    {
        ArgumentNullException.ThrowIfNull(middlewares);
        
        var allMiddlewares = middlewares.ToArray();
        
        _valueMiddlewares = allMiddlewares.OfType<IValueMappingMiddleware>().ToArray();
        _rowStateMiddlewares = allMiddlewares.OfType<IRowStateMappingMiddleware>().ToArray();
        _keysMiddlewares = allMiddlewares.OfType<IKeysMappingMiddleware>().ToArray();
    }

    /// <summary>
    /// Возвращает смапленное состояние строки
    /// </summary>
    public RowState GetRowState(IRow source) => GetRowStateChain(source, 0);

    /// <summary>
    /// Выполняет по цепочке все миддлвари по получению смапленного состояния строки
    /// </summary>
    private RowState GetRowStateChain(IRow source, int index)
    {
        if (index >= _valueMiddlewares.Length)
            return source.State;

        RowState NextDelegate(IRow s) => GetRowStateChain(s, index + 1);

        return _rowStateMiddlewares[index].GetRowState(source, NextDelegate);
    }

    /// <summary>
    /// Возвращает список ключей для строки
    /// </summary>
    public IEnumerable<string> GetKeys(IRow source) => GetKeysChain(source, 0);

    /// <summary>
    /// Выполняет по цепочке все миддлвари по получению смапленных ключей строки
    /// </summary>
    private IEnumerable<string> GetKeysChain(IRow source, int index)
    {
        if (index >= _valueMiddlewares.Length)
            return source.Keys;

        IEnumerable<string> NextDelegate(IRow s) => GetKeysChain(s, index + 1);

        return _keysMiddlewares[index].GetKeys(source, NextDelegate);
    }
    
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
        if (index >= _valueMiddlewares.Length)
            return source.TryGetValue(columnKey, out value);

        bool NextDelegate(IRow s, string k, out object? v) => TryGetValueChain(s, k, out v, index + 1);

        return _valueMiddlewares[index].TryGetValue(source, columnKey, out value, NextDelegate);
    }
}