using ThisShard.Database.Core.Models;
using ThisShard.Database.Core.Models.Rows;

namespace ThisShard.Database.Core.Mappers;

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
    /// Пытается получить смапленное значение ячейки у строки
    /// </summary>
    public bool TryGetValue(IRow source, string columnKey, out object? value) => source.TryGetValue(columnKey, out value);
}