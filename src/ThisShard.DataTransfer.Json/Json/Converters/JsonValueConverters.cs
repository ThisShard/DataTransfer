using System.Text.Json;
using ThisShard.Database.Core.Converters.Handlers;
using ThisShard.Database.Infrastructure.Json.Helpers;

namespace ThisShard.Database.Infrastructure.Json.Converters;

/// <summary>
/// Конвертеры по умолчанию для Json
/// </summary>
public static class JsonValueConverters
{
    /// <summary>
    /// Список конвертеров по умолчанию для Json
    /// </summary>
    public static IReadOnlyList<IValueConverterHandler> Default { get; } =
    [
        ValueConverterHandler.CreateTyped<TimeSpan, string>(val=>val.ToString("c")),
        ValueConverterHandler.CreateTyped<DateOnly, string>(val=>val.ToString("O")),
        ValueConverterHandler.CreateTyped<TimeOnly, string>(val=>val.ToString("O")),
        ValueConverterHandler.CreateTyped<char, string>(val=>val.ToString()),
        ValueConverterHandler.CreateTo<string>(
            (val, _) => JsonSerializer.Serialize(val), 
            type => !JsonWriterHelper.KnownTypes.Contains(type)
        )
    ];
}