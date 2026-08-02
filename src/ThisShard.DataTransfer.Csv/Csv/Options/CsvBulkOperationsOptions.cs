using ThisShard.Database.Core.Converters;
using ThisShard.Database.Core.Models.Rows;
using ThisShard.Database.Infrastructure.Csv.Converters;
using ThisShard.Database.Infrastructure.Csv.Tables;

namespace ThisShard.Database.Infrastructure.Csv.Options;

/// <summary>
/// Настройки Bulk операций для Csv
/// </summary>
public record CsvBulkOperationsOptions
{
    /// <summary>
    /// Настройки по умолчанию
    /// </summary>
    public static CsvBulkOperationsOptions Default { get; set; } = new();
    
    /// <summary>
    /// Менеджер таблиц Json
    /// </summary>
    public ICsvTableProvider TableProvider { get; init; } = new CsvTableProvider();

    /// <summary>
    /// Конвертер значений
    /// </summary>
    public IValueConverter? ValueConverter { get; init; } = new ValueConverter(CsvValueConverters.Default);

    /// <summary>
    /// Фильтр строк
    /// </summary>
    public Func<IRow, bool> RowFilter { get; init; } = row => true;
    
    /// <summary>
    /// Имя свойства состояния строки
    /// </summary>
    public string? RowStatePropertyName { get; init; } = null;
}