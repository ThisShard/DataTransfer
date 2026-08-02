using ThisShard.Database.Core.Models;
using ThisShard.Database.Core.Models.Tables;
using ThisShard.Database.Infrastructure.Csv.Models;

namespace ThisShard.Database.Infrastructure.Csv.Tables;

/// <summary>
/// Провайдер Json таблиц
/// </summary>
public class CsvTableProvider : ICsvTableProvider
{
    /// <summary>
    /// Резолвер имени свойства по умолчанию
    /// </summary>
    public static Func<string, string> DefaultPropertyNameResolver { get; set; } = val => val;
    
    private readonly Func<string, string> _propertyNameResolver;

    public CsvTableProvider() : this(DefaultPropertyNameResolver)
    {
    }

    public CsvTableProvider(Func<string, string> propertyNameResolver)
    {
        _propertyNameResolver = propertyNameResolver ?? throw new ArgumentNullException(nameof(propertyNameResolver));
    }

    /// <summary>
    /// Возвращает таблицу для типа
    /// </summary>
    public CsvTable GetTable(Type type)
    {
        return new CsvTable()
        {
            Key = type.FullName!,
            Columns = type.GetProperties().Select(x => new CsvColumn()
            {
                Key = x.Name,
                Name = _propertyNameResolver(x.Name),
            }).ToList()
        };
    }
    
    #region ConvertTable

    /// <summary>
    /// Конвертирует таблицу в Csv
    /// </summary>
    public CsvTable? ConvertTable(ITable table)
    {
        var columns = ConvertColumns(table).ToArray();
        return new CsvTable()
        {
            Key = table.Key,
            Columns = columns,
        };
    }

    /// <summary>
    /// Конвертирует колонки в Csv
    /// </summary>
    private IEnumerable<CsvColumn> ConvertColumns(ITable table)
    {
        foreach (var column in table.Columns)
        {
            yield return new CsvColumn()
            {
                Key = column.Key,
                Name = _propertyNameResolver(column.RawName),
            };
        }
    }

    #endregion
}