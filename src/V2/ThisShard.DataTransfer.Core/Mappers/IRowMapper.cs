using ThisShard.Database.Models.Rows;

namespace ThisShard.Database.Mappers;

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
    /// Возвращает список ключей для строки
    /// </summary>
    IEnumerable<string> GetKeys(IRow source);
    
    /// <summary>
    /// Пытается получить смапленное значение ячейки у строки
    /// </summary>
    bool TryGetValue(IRow source, string columnKey, out object? value);
}