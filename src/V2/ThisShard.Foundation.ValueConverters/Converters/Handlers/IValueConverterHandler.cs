namespace ThisShard.Foundation.Converters.Handlers;

/// <summary>
/// Обработчик конвертации
/// </summary>
public interface IValueConverterHandler
{
    /// <summary>
    /// Исходный тип
    /// </summary>
    Type? SourceType { get; }
    
    /// <summary>
    /// Конечный тип
    /// </summary>
    Type? TargetType { get; }
    
    /// <summary>
    /// Проверяет, может ли конвертировать указанные типы
    /// </summary>
    bool CanConvert(Type sourceType, Type targetType);
    
    /// <summary>
    /// Конвертирует значение
    /// </summary>
    object? Convert(object? value, Type sourceType, Type targetType);
}