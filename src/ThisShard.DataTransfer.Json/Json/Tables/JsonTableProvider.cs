using ThisShard.Database.Core.Models;
using ThisShard.Database.Core.Models.Tables;
using ThisShard.Database.Infrastructure.Json.Helpers;
using ThisShard.Database.Infrastructure.Json.Models;

namespace ThisShard.Database.Infrastructure.Json.Tables;

/// <summary>
/// Провайдер Json таблиц
/// </summary>
public class JsonTableProvider : IJsonTableProvider
{
    /// <summary>
    /// Резолвер имени свойства по умолчанию
    /// </summary>
    public static Func<string, string> DefaultPropertyNameResolver { get; set; } =
        JsonPropertyNameResolvers.NonConvertingPropertyNameResolver;
    
    private readonly Func<string, string> _propertyNameResolver;

    public JsonTableProvider() : this(DefaultPropertyNameResolver)
    {
    }

    public JsonTableProvider(Func<string, string> propertyNameResolver)
    {
        _propertyNameResolver = propertyNameResolver ?? throw new ArgumentNullException(nameof(propertyNameResolver));
    }

    /// <summary>
    /// Возвращает таблицу для типа
    /// </summary>
    public JsonTable GetTable(Type type)
    {
        return new JsonTable()
        {
            Key = type.FullName!,
            Columns = type.GetProperties().Select(x => new JsonColumn()
            {
                Key = x.Name,
                Name = _propertyNameResolver(x.Name),
                Type = x.PropertyType
            }).ToList()
        };
    }
    
    #region ConvertTable

    /// <summary>
    /// Конвертирует таблицу в Json
    /// </summary>
    public JsonTable? ConvertTable(ITable table)
    {
        var columns = ConvertColumns(table).ToArray();
        return new JsonTable()
        {
            Key = table.Key,
            Columns = columns,
        };
    }

    /// <summary>
    /// Конвертирует колонки в Json
    /// </summary>
    private IEnumerable<JsonColumn> ConvertColumns(ITable table)
    {
        foreach (var column in table.Columns)
        {
            yield return new JsonColumn()
            {
                Key = column.Key,
                Name = _propertyNameResolver(column.RawName),
                Type = JsonWriterHelper.GetJsonColumnType(column.Type),
            };
        }
    }

    #endregion
}