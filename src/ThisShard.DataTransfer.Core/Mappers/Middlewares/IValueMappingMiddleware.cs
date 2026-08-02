using ThisShard.Database.Core.Models;
using ThisShard.Database.Core.Models.Rows;

namespace ThisShard.Database.Core.Mappers.Middlewares;

/// <summary>
/// Миддлварь мэппинга строк
/// </summary>
public interface IValueMappingMiddleware
{
    /// <summary>
    /// Пытается получить смапленное значение ячейки у строки
    /// </summary>
    bool TryGetValue(IRow source, string columnKey, out object? value, TryGetValueDelegate next);
}

/// <summary>
/// Делегат получения состояния для строки
/// </summary>
public delegate RowState GetRowStateDelegate(IRow source);

/// <summary>
/// Делегат получения значения для строки
/// </summary>
public delegate bool TryGetValueDelegate(IRow source, string columnKey, out object? value);