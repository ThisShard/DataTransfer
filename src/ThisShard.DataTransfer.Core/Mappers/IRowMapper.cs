using ThisShard.Database.Core.Models;
using ThisShard.Database.Core.Models.Rows;

namespace ThisShard.Database.Core.Mappers;

/// <summary>
/// Маппер строк
/// </summary>
public interface IRowMapper
{
    /// <summary>
    /// Возвращает смапленное состояние строки
    /// </summary>
    RowState GetRowState(IRow source);
    
    /// <summary>
    /// Пытается получить смапленное значение ячейки у строки
    /// </summary>
    bool TryGetValue(IRow source, string columnKey, out object? value);
}