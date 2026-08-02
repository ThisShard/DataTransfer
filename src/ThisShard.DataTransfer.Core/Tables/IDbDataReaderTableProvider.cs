using System.Data.Common;
using ThisShard.Database.Core.Models;
using ThisShard.Database.Core.Models.Tables;

namespace ThisShard.Database.Core.Tables;

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