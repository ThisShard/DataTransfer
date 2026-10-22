using Npgsql;
using ThisShard.Database.Tables;
using ThisShard.Database.Writers.CommandFilters;
using ThisShard.Foundation.Converters;
using ThisShard.Foundation.Converters.Handlers;

namespace ThisShard.Database.Options;

/// <summary>
/// Настройки Bulk операций для Postgresql
/// </summary>
public record NpgsqlBulkOperationsOptions
{
    /// <summary>
    /// Настройки по умолчанию
    /// </summary>
    public static NpgsqlBulkOperationsOptions Default { get; set; } = new();
    
    /// <summary>
    /// Менеджер временной таблицы
    /// </summary>
    public IPgStagingTableManager StagingTableManager { get; init; } = new PgStagingTableManager();
    
    /// <summary>
    /// Провайдер схем таблиц
    /// </summary>
    public IPgTableManager TableManager { get; init; } = new PgTableManager();

    /// <summary>
    /// Конвертер значений
    /// </summary>
    public IValueConverter? ValueConverter { get; init; } = new ValueConverter(DefaultValueConverterHandlers.AllHandlers);

    /// <summary>
    /// Минимальное количество строк для использования Bulk операций
    /// </summary>
    public int BulkMinRows { get; init; } = 1000;
    
    /// <summary>
    /// Максимальный размер буфера для Bulk операций
    /// </summary>
    public int BulkBufferSize { get; init; } = 10000;
    
    /// <summary>
    /// Максимальный размер буфера для Batch операций
    /// </summary>
    public int BatchBufferSize { get; init; } = 1000;
    
    /// <summary>
    /// Настройки устойчивых операций
    /// </summary>
    public SustainableOperationsOptions<NpgsqlConnection>? SustainableOptions { get; init; }
    
    /// <summary>
    /// Фабрика дополнительных фильтров для команд
    /// </summary>
    public Func<CommandFilterFactoryContext, CommandFilter<NpgsqlParameter>>? CommandFilterFactory { get; init; }

}