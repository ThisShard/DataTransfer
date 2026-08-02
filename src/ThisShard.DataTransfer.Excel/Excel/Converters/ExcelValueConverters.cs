using System.Text.Json;
using ThisShard.Database.Core.Converters.Handlers;
using ThisShard.Database.Infrastructure.Excel.Helpers;

namespace ThisShard.Database.Infrastructure.Excel.Converters;

/// <summary>
/// Конвертеры по умолчанию для Json
/// </summary>
public static class ExcelValueConverters
{
    /// <summary>
    /// Список конвертеров по умолчанию для Json
    /// </summary>
    public static IReadOnlyList<IValueConverterHandler> Default { get; } =
    [
        ValueConverterHandler.CreateTyped<Guid, string>(val=>val.ToString()),
        ValueConverterHandler.CreateTyped<DateTimeOffset, string>(val=>val.ToString("O")),
        ValueConverterHandler.CreateTyped<byte[], string>(Convert.ToBase64String),
        
        ValueConverterHandler.CreateTyped<long, decimal>(val => val),
        ValueConverterHandler.CreateTyped<ulong, decimal>(val => val),
        ValueConverterHandler.CreateTyped<uint, decimal>(val => val),
        
        ValueConverterHandler.CreateTyped<float, double>(val => val),
        
        ValueConverterHandler.CreateTyped<short, int>(val => val),
        ValueConverterHandler.CreateTyped<ushort, int>(val => val),
        ValueConverterHandler.CreateTyped<byte, int>(val => val),
        ValueConverterHandler.CreateTyped<sbyte, int>(val => val),
        
        ValueConverterHandler.CreateTyped<TimeSpan, string>(val=>val.ToString("c")),
        ValueConverterHandler.CreateTyped<DateOnly, string>(val=>val.ToString("O")),
        ValueConverterHandler.CreateTyped<TimeOnly, string>(val=>val.ToString("O")),
        
        ValueConverterHandler.CreateTyped<char, string>(val=>val.ToString()),
        ValueConverterHandler.CreateTo<string>(
            (val, _) => JsonSerializer.Serialize(val), 
            type => !ExcelWriterHelper.TypesMap.Keys.Contains(type)
        )
    ];
}