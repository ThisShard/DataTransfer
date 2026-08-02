using ThisShard.Database.Core.Models;
using ThisShard.Database.Core.Models.Tables;
using ThisShard.Database.Infrastructure.Json.Models;

namespace ThisShard.Database.Infrastructure.Json.Tables;

/// <summary>
/// Провайдер Json таблиц
/// </summary>
public interface IJsonTableProvider
{
    /// <summary>
    /// Возвращает таблицу для типа
    /// </summary>
    JsonTable GetTable(Type type);
    
    /// <summary>
    /// Конвертирует таблицу в Json
    /// </summary>
    JsonTable? ConvertTable(ITable table);
}