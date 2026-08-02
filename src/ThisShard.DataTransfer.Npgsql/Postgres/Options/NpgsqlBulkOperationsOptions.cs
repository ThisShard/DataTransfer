using System.Data;
using Npgsql;
using ThisShard.Database.Core.Converters;
using ThisShard.Database.Core.Converters.Handlers;
using ThisShard.Database.Core.Models.Filters;
using ThisShard.Database.Core.Options;
using ThisShard.Database.Infrastructure.Postgres.Tables;

namespace ThisShard.Database.Infrastructure.Postgres.Options;

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
    public IValueConverter? ValueConverter { get; init; } = new ValueConverter(DefaultValueConverterHandlers.Default);

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