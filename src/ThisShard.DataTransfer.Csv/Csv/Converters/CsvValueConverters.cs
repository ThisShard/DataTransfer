using System.Text.Json;
using ThisShard.Database.Core.Converters.Handlers;

namespace ThisShard.Database.Infrastructure.Csv.Converters;

/// <summary>
/// Конвертеры по умолчанию для Csv
/// </summary>
public static class CsvValueConverters
{
    /// <summary>
    /// Список конвертеров по умолчанию для Csv
    /// </summary>
    public static IReadOnlyList<IValueConverterHandler> Default { get; } =
        DefaultValueConverterHandlers.ToStringHandlers;
}