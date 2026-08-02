using ThisShard.Database.Core.Models;
using ThisShard.Database.Core.Models.Tables;

namespace ThisShard.Database.Core.Schemas;

/// <summary>
/// Провайдер схем таблиц
/// </summary>
public interface ITableSchemaProvider
{
    /// <summary>
    /// Возвращает схему таблицы
    /// </summary>
    ITableSchema GetSchema(ITable table);
}