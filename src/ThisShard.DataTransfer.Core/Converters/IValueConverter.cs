using ThisShard.Database.Core.Models;
using ThisShard.Database.Core.Models.Columns;

namespace ThisShard.Database.Core.Converters;

/// <summary>
/// Конвертер типа значения под подходящую колонку
/// </summary>
public interface IValueConverter
{
    /// <summary>
    /// Конвертирует значение в соответствии с типом колонки
    /// </summary>
    object? Convert(object? value, IColumn column);
}