using ThisShard.Database.Models.Rows;

namespace ThisShard.Database.Mappers;

/// <summary>
/// Маппер строк по умолчанию
/// </summary>
public class DefaultRowMapper : IRowMapper
{
    /// <summary>
    /// Возвращает смапленное состояние строки
    /// </summary>
    public RowState GetRowState(IRow source) => source.State;

    /// <summary>
    /// Возвращает список ключей для строки
    /// </summary>
    public IEnumerable<string> GetKeys(IRow source) => source.Keys;

    /// <summary>
    /// Пытается получить смапленное значение ячейки у строки
    /// </summary>
    public bool TryGetValue(IRow source, string columnKey, out object? value) => source.TryGetValue(columnKey, out value);
}