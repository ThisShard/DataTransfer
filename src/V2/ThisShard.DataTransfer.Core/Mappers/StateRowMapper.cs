using ThisShard.Database.Models.Rows;

namespace ThisShard.Database.Mappers;

/// <summary>
/// Маппер состояния строки
/// </summary>
public class StateRowMapper : IRowMapper
{
    private readonly Func<RowState, RowState> _stateMapper;

    public StateRowMapper(Func<RowState, RowState> stateMapper)
    {
        _stateMapper = stateMapper ?? throw new ArgumentNullException(nameof(stateMapper));
    }

    /// <summary>
    /// Возвращает смапленное состояние строки
    /// </summary>
    public RowState GetRowState(IRow source) => _stateMapper(source.State);

    /// <summary>
    /// Возвращает список ключей для строки
    /// </summary>
    public IEnumerable<string> GetKeys(IRow source) => source.Keys;

    /// <summary>
    /// Пытается получить смапленное значение ячейки у строки
    /// </summary>
    public bool TryGetValue(IRow source, string columnKey, out object? value) => source.TryGetValue(columnKey, out value);
}