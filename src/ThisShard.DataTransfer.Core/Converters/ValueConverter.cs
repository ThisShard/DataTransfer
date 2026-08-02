using ThisShard.Database.Core.Converters.Handlers;
using ThisShard.Database.Core.Models;
using ThisShard.Database.Core.Models.Columns;

namespace ThisShard.Database.Core.Converters;

/// <summary>
/// Конвертер значений использующий хендлеры
/// </summary>
public class ValueConverter : IValueConverter
{
    private readonly ILookup<(Type, Type), IValueConverterHandler> _strictHandlers;
    private readonly ILookup<Type, IValueConverterHandler> _targetTypeHandlers;
    private readonly ILookup<Type, IValueConverterHandler> _sourceTypeHandlers;
    private readonly IValueConverterHandler[] _fallbackHandlers;

    public ValueConverter(IReadOnlyCollection<IValueConverterHandler> handlers)
    {
        _strictHandlers = handlers.Where(x=>x is { SourceType: not null, TargetType: not null })
            .ToLookup(x=>(x.SourceType!, x.TargetType!));
        _targetTypeHandlers = handlers.Where(x => x is { SourceType: null, TargetType: not null })
            .ToLookup(x => x.TargetType!);
        _sourceTypeHandlers = handlers.Where(x => x is { SourceType: not null, TargetType: null })
            .ToLookup(x => x.SourceType!);
        _fallbackHandlers = handlers.Where(x => x is { SourceType: null, TargetType: null }).ToArray();
    }

    /// <summary>
    /// Конвертирует значение в соответствии с типом колонки
    /// </summary>
    public object? Convert(object? value, IColumn column)
    {
        if (value == null || value == DBNull.Value)
            return PostConvertValue(null, column);

        var valueType = value.GetType();
        
        if (valueType == column.Type)
            return value;
        
        var handler = _strictHandlers[(valueType, column.Type)].FirstOrDefault(x=>x.CanConvert(valueType, column.Type))
            ?? _targetTypeHandlers[column.Type].FirstOrDefault(x=>x.CanConvert(valueType, column.Type))
            ?? _sourceTypeHandlers[valueType].FirstOrDefault(x=>x.CanConvert(valueType, column.Type))
            ?? _fallbackHandlers.FirstOrDefault(x=>x.CanConvert(valueType, column.Type));
        
        var convertedValue = handler == null 
            ? value 
            : handler.Convert(value, valueType, column.Type);

        return PostConvertValue(convertedValue, column);
    }

    /// <summary>
    /// Постконвертация значения
    /// </summary>
    private object? PostConvertValue(object? value, IColumn column)
    {
        if (column.IsNullable || value != null)
            return value;

        if (column.Type == typeof(string))
            return string.Empty;

        if (column.Type.IsValueType)
            return Activator.CreateInstance(column.Type);

        return value;
    }
}