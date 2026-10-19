using ThisShard.Foundation.Converters.Handlers;

namespace ThisShard.Foundation.Converters;

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
    /// Конвертирует значение в соответствии с типом и признаком нуллабла
    /// </summary>
    public object? Convert(object? value, Type underlyingType, bool nullable) => ConvertInternal(value, underlyingType, nullable);

    /// <summary>
    /// Внутренний метод конвертации
    /// </summary>
    private object? ConvertInternal(object? value, Type type, bool nullable)
    {
        if (value == null || value == DBNull.Value)
            return PostConvertValue(null, type, nullable);

        var valueType = value.GetType();
        
        if (valueType == type)
            return value;
        
        var handler = _strictHandlers[(valueType, type)].FirstOrDefault(x=>x.CanConvert(valueType, type))
            ?? _targetTypeHandlers[type].FirstOrDefault(x=>x.CanConvert(valueType, type))
            ?? _sourceTypeHandlers[valueType].FirstOrDefault(x=>x.CanConvert(valueType, type))
            ?? _fallbackHandlers.FirstOrDefault(x=>x.CanConvert(valueType, type));
        
        var convertedValue = handler == null 
            ? value 
            : handler.Convert(value, valueType, type);

        return PostConvertValue(convertedValue, type, nullable);
    }

    /// <summary>
    /// Постконвертация значения
    /// </summary>
    private object? PostConvertValue(object? value, Type type, bool nullable)
    {
        if (nullable || value != null)
            return value;

        if (type == typeof(string))
            return string.Empty;

        if (type.IsValueType)
            return Activator.CreateInstance(type);

        return value;
    }
}