using ThisShard.Database.Models.Rows;

namespace ThisShard.Database.Mappers.Middlewares;

/// <summary>
/// Миддлварь мэппинга ключей
/// </summary>
public interface IKeysMappingMiddleware : IRowMappingMiddleware
{
    /// <summary>
    /// Пытается получить смапленные ключи строки
    /// </summary>
    IEnumerable<string> GetKeys(IRow source, GetKeysDelegate next);
}

/// <summary>
/// Делегат получения ключей строки
/// </summary>
public delegate IEnumerable<string> GetKeysDelegate(IRow source);