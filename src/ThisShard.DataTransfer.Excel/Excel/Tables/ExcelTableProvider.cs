using ThisShard.Database.Core.Models;
using ThisShard.Database.Core.Models.Tables;
using ThisShard.Database.Infrastructure.Excel.Helpers;
using ThisShard.Database.Infrastructure.Excel.Models;

namespace ThisShard.Database.Infrastructure.Excel.Tables;

/// <summary>
/// Провайдер Json таблиц
/// </summary>
public class ExcelTableProvider : IExcelTableProvider
{
    /// <summary>
    /// Резолвер имени свойства по умолчанию
    /// </summary>
    public static Func<string, string> DefaultPropertyNameResolver { get; set; } = val => val;
    
    private readonly Func<string, string> _propertyNameResolver;

    public ExcelTableProvider() : this(DefaultPropertyNameResolver)
    {
    }

    public ExcelTableProvider(Func<string, string> propertyNameResolver)
    {
        _propertyNameResolver = propertyNameResolver ?? throw new ArgumentNullException(nameof(propertyNameResolver));
    }

    /// <summary>
    /// Возвращает таблицу для типа
    /// </summary>
    public ExcelTable GetTable(Type type)
    {
        return new ExcelTable()
        {
            Key = type.FullName!,
            Columns = type.GetProperties().Select(x => new ExcelColumn()
            {
                Key = x.Name,
                Name = _propertyNameResolver(x.Name),
                Type = ExcelWriterHelper.GetExcelColumnType(x.PropertyType)
            }).ToList()
        };
    }
    
    #region ConvertTable

    /// <summary>
    /// Конвертирует таблицу в Excel
    /// </summary>
    public ExcelTable? ConvertTable(ITable table)
    {
        var columns = ConvertColumns(table).ToArray();
        return new ExcelTable()
        {
            Key = table.Key,
            Columns = columns,
        };
    }

    /// <summary>
    /// Конвертирует колонки в Excel
    /// </summary>
    private IEnumerable<ExcelColumn> ConvertColumns(ITable table)
    {
        foreach (var column in table.Columns)
        {
            yield return new ExcelColumn()
            {
                Key = column.Key,
                Name = _propertyNameResolver(column.RawName),
                Type = ExcelWriterHelper.GetExcelColumnType(column.Type)
            };
        }
    }

    #endregion
}