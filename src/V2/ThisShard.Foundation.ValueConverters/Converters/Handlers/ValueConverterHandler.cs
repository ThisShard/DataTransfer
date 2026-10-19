namespace ThisShard.Foundation.Converters.Handlers;

/// <summary>
/// Хендлер
/// </summary>
public class ValueConverterHandler : IValueConverterHandler
{
    private readonly Func<Type, Type, bool> _canConvert;
    private readonly Func<object?, Type,Type,object?> _convert;

    /// <summary>
    /// Исходный тип
    /// </summary>
    public Type? SourceType { get; }

    /// <summary>
    /// Конечный тип
    /// </summary>
    public Type? TargetType { get; }
    
    private ValueConverterHandler(Type? sourceType, Type? targetType, Func<Type, Type, bool> canConvert, Func<object?, Type, Type, object?> convert)
    {
        SourceType = sourceType;
        TargetType = targetType;
        _canConvert = canConvert;
        _convert = convert;
    }

    /// <summary>
    /// Проверяет, может ли конвертировать указанные типы
    /// </summary>
    public bool CanConvert(Type sourceType, Type targetType) => _canConvert(sourceType, targetType);

    /// <summary>
    /// Конвертирует значение
    /// </summary>
    public object? Convert(object? value, Type sourceType, Type targetType) => _convert(value, sourceType, targetType);

    /// <summary>
    /// Создает типизированный конвертер для структур с возможностью возврата null
    /// </summary>
    public static ValueConverterHandler CreateTypedNullable<TSrc, TDest>(Func<TSrc, TDest?> convert) where TDest : struct => new(
        sourceType: typeof(TSrc),
        targetType: typeof(TDest),
        canConvert: (src, dest) => src == typeof(TSrc) && dest == typeof(TDest),
        convert: (value, _, _) => convert((TSrc)value!));
    
    /// <summary>
    /// Создает типизированный конвертер
    /// </summary>
    public static ValueConverterHandler CreateTyped<TSrc, TDest>(Func<TSrc, TDest?> convert) => new(
        sourceType: typeof(TSrc),
        targetType: typeof(TDest),
        canConvert: (src, dest) => src == typeof(TSrc) && dest == typeof(TDest),
        convert: (value, _, _) => convert((TSrc)value!));
    
    /// <summary>
    /// Создает конвертер в указанный тип
    /// </summary>
    public static ValueConverterHandler CreateTo<TDest>(Func<object, Type, TDest?> convert, Func<Type, bool>? typeFilter = null) => new(
        sourceType: null,
        targetType: typeof(TDest),
        canConvert: (src, dest) => dest == typeof(TDest) && (typeFilter == null || typeFilter(src)),
        convert: (value, src, _) => convert(value!, src));
    
    /// <summary>
    /// Создает конвертер в указанный тип для структур с возможностью возврата null
    /// </summary>
    public static ValueConverterHandler CreateToNullable<TDest>(Func<object, Type, TDest?> convert, Func<Type, bool>? typeFilter = null) where TDest : struct => new(
        sourceType: null,
        targetType: typeof(TDest),
        canConvert: (src, dest) => dest == typeof(TDest) && (typeFilter == null || typeFilter(src)),
        convert: (value, src, _) => convert(value!, src));
    
    /// <summary>
    /// Создает конвертер из указанного типа
    /// </summary>
    public static ValueConverterHandler CreateFrom<TSrc>(Func<TSrc, Type, object?> convert, Func<Type, bool>? typeFilter = null) => new(
        sourceType: typeof(TSrc),
        targetType: null,
        canConvert: (src, dest) => src == typeof(TSrc) && (typeFilter == null || typeFilter(dest)),
        convert: (value, _, dest) => convert((TSrc)value!, dest));
    
    /// <summary>
    /// Создает конвертер
    /// </summary>
    public static ValueConverterHandler Create(Func<object, Type, Type, object?> convert, Func<Type, Type, bool>? typeFilter = null) => new(
        sourceType: null,
        targetType: null,
        canConvert: (src, dest) => typeFilter == null || typeFilter(src, dest),
        convert: (value, src, dest) => convert(value!, src, dest));
    
    
    
}