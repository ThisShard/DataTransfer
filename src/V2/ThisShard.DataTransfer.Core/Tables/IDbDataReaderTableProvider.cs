using System.Data.Common;
using ThisShard.Database.Models.Tables;

namespace ThisShard.Database.Tables;

/// <summary>
/// Провайдер таблицы из DbDataReader
/// </summary>
public interface IDbDataReaderTableProvider
{
    /// <summary>
    /// Возвращает схему таблицы для указанного пути
    /// </summary>
    Task<Table?> GetTable(DbDataReader reader, string[] path);
}