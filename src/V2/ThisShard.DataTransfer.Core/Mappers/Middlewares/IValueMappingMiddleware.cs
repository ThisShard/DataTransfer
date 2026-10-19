using ThisShard.Database.Models.Rows;

namespace ThisShard.Database.Mappers.Middlewares;

/// <summary>
/// Миддлварь мэппинга строк
/// </summary>
public interface IValueMappingMiddleware : IRowMappingMiddleware
{
    /// <summary>
    /// Пытается получить смапленное значение ячейки у строки
    /// </summary>
    bool TryGetValue(IRow source, string columnKey, out object? value, TryGetValueDelegate next);
}

/// <summary>
/// Делегат получения значения для строки
/// </summary>
public delegate bool TryGetValueDelegate(IRow source, string columnKey, out object? value);