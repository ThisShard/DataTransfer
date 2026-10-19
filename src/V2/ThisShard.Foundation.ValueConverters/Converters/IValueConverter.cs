namespace ThisShard.Foundation.Converters;

/// <summary>
/// Конвертер типа значения
/// </summary>
public interface IValueConverter
{
    /// <summary>
    /// Конвертирует значение в соответствии с типом и признаком нуллабла
    /// </summary>
    object? Convert(object? value, Type underlyingType, bool nullable);
}