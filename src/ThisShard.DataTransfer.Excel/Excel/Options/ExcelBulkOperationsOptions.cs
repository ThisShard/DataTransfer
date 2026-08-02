using ThisShard.Database.Core.Converters;
using ThisShard.Database.Core.Models.Rows;
using ThisShard.Database.Infrastructure.Excel.Converters;
using ThisShard.Database.Infrastructure.Excel.Tables;

namespace ThisShard.Database.Infrastructure.Excel.Options;

/// <summary>
/// Настройки Bulk операций для Excel
/// </summary>
public record ExcelBulkOperationsOptions
{
    /// <summary>
    /// Настройки по умолчанию
    /// </summary>
    public static ExcelBulkOperationsOptions Default { get; set; } = new();
    
    /// <summary>
    /// Менеджер таблиц Json
    /// </summary>
    public IExcelTableProvider TableProvider { get; init; } = new ExcelTableProvider();

    /// <summary>
    /// Конвертер значений
    /// </summary>
    public IValueConverter? ValueConverter { get; init; } = new ValueConverter(ExcelValueConverters.Default);

    /// <summary>
    /// Фильтр строк
    /// </summary>
    public Func<IRow, bool> RowFilter { get; init; } = row => true;
    
    /// <summary>
    /// Имя свойства состояния строки
    /// </summary>
    public string? RowStatePropertyName { get; init; } = null;
    
    /// <summary>
    /// Стили ячеек
    /// </summary>
    public ExcelStyleOptions Styles { get; init; } = new();
}