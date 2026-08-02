using ThisShard.Database.Core.Models;
using ThisShard.Database.Core.Models.Tables;

namespace ThisShard.Database.Core.Schemas;

/// <summary>
/// Провайдер схем временных таблиц
/// </summary>
public interface IStagingTableSchemaProvider
{
    /// <summary>
    /// Возвращает схему временной таблицы
    /// </summary>
    IStagingTableSchema GetSchema(IStagingTable table);
}