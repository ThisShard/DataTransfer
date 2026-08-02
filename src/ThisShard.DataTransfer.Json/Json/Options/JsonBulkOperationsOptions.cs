using ThisShard.Database.Core.Converters;
using ThisShard.Database.Core.Models;
using ThisShard.Database.Core.Models.Rows;
using ThisShard.Database.Infrastructure.Json.Converters;
using ThisShard.Database.Infrastructure.Json.Tables;

namespace ThisShard.Database.Infrastructure.Json.Options;

/// <summary>
/// Настройки Bulk операций для Json
/// </summary>
public record JsonBulkOperationsOptions
{
    /// <summary>
    /// Настройки по умолчанию
    /// </summary>
    public static JsonBulkOperationsOptions Default { get; set; } = new();
    
    /// <summary>
    /// Менеджер таблиц Json
    /// </summary>
    public IJsonTableProvider TableProvider { get; init; } = new JsonTableProvider();

    /// <summary>
    /// Конвертер значений
    /// </summary>
    public IValueConverter? ValueConverter { get; init; } = new ValueConverter(JsonValueConverters.Default);

    /// <summary>
    /// Фильтр строк
    /// </summary>
    public Func<IRow, bool> RowFilter { get; init; } = row => true;
    
    /// <summary>
    /// Имя свойства состояния строки
    /// </summary>
    public string? RowStatePropertyName { get; init; } = null;
}