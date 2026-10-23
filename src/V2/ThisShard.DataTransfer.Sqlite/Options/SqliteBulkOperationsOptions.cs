using Microsoft.Data.Sqlite;
using ThisShard.Database.Converters;
using ThisShard.Database.Tables;
using ThisShard.Database.Writers.CommandFilters;
using ThisShard.Foundation.Converters;

namespace ThisShard.Database.Options;

/// <summary>
/// Настройки Bulk операций для Sqlite
/// </summary>
public record SqliteBulkOperationsOptions
{
    /// <summary>
    /// Настройки по умолчанию
    /// </summary>
    public static SqliteBulkOperationsOptions Default { get; set; } = new();
    
    /// <summary>
    /// Менеджер таблиц Sqlite
    /// </summary>
    public ISqliteTableManager TableManager { get; init; } = new SqliteTableManager();

    /// <summary>
    /// Конвертер значений
    /// </summary>
    public IValueConverter? ValueConverter { get; init; } = new ValueConverter(SqliteValueConverters.Default);
    
    /// <summary>
    /// Максимальный размер буфера для Batch операций
    /// </summary>
    public int BatchBufferSize { get; init; } = 1000;
    
    /// <summary>
    /// Настройки устойчивых операций
    /// </summary>
    public SustainableOperationsOptions<SqliteConnection>? SustainableOptions { get; init; }
    
    /// <summary>
    /// Фабрика дополнительных фильтров для команд
    /// </summary>
    public Func<CommandFilterFactoryContext, CommandFilter<SqliteParameter>>? CommandFilterFactory { get; init; }
}