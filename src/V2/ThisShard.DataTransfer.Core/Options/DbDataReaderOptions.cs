using ThisShard.Database.Tables;

namespace ThisShard.Database.Options;

/// <summary>
/// Настройки Bulk операций для Sqlite
/// </summary>
public class DbDataReaderOptions
{
    /// <summary>
    /// Настройки по умолчанию
    /// </summary>
    public static DbDataReaderOptions Default { get; set; } = new();
    
    /// <summary>
    /// Провайдер таблиц из датаридера
    /// </summary>
    public IDbDataReaderTableProvider DbDataReaderTableProvider { get; set; } = new DbDataReaderTableProvider();
}