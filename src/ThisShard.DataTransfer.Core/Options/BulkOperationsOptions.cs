using ThisShard.Database.Core.Tables;

namespace ThisShard.Database.Core.Options;

/// <summary>
/// Настройки Bulk операций для Sqlite
/// </summary>
public class BulkOperationsOptions
{
    /// <summary>
    /// Настройки по умолчанию
    /// </summary>
    public static BulkOperationsOptions Default { get; set; } = new();
    
    /// <summary>
    /// Провайдер таблиц из датаридера
    /// </summary>
    public IDbDataReaderTableProvider DbDataReaderTableProvider { get; set; } = new DbDataReaderTableProvider();
}