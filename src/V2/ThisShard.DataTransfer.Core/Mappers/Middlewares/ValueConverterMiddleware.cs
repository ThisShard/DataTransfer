using ThisShard.Database.Models.Columns;
using ThisShard.Database.Models.Rows;
using ThisShard.Database.Models.Tables;
using ThisShard.Foundation.Converters;

namespace ThisShard.Database.Mappers.Middlewares;

/// <summary>
/// Миддлварь преобразования значений использующий конвертер
/// </summary>
public class ValueConverterMiddleware : IValueMappingMiddleware
{
    private readonly IValueConverter _converter;
    private readonly IReadOnlyDictionary<string, IColumn> _columnsMap;

    public ValueConverterMiddleware(IValueConverter converter, ITable table)
    {
        _converter = converter;
        _columnsMap = table.Columns.ToDictionary(x => x.Key);
    }

    /// <summary>
    /// Пытается получить смапленное значение ячейки у строки
    /// </summary>
    public bool TryGetValue(IRow source, string columnKey, out object? value, TryGetValueDelegate next)
    {
        if (!next(source, columnKey, out value))
            return false;

        if (!_columnsMap.TryGetValue(columnKey, out var column))
            return true;
        
        value = _converter.Convert(value, column.Type, column.IsNullable);
        return true;
    }
}