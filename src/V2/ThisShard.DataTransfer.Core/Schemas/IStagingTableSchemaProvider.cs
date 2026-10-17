using ThisShard.Database.Models.Tables;

namespace ThisShard.Database.Schemas;

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