using System.Text.Json;
using ThisShard.Database.Core.Converters.Handlers;
using ThisShard.Database.Infrastructure.Sqlite.Helpers;

namespace ThisShard.Database.Infrastructure.Sqlite.Converters;

/// <summary>
/// Конвертеры по умолчанию для Sqlite
/// </summary>
public static class SqliteValueConverters
{
    /// <summary>
    /// Список конвертеров по умолчанию для Sqlite
    /// </summary>
    public static IReadOnlyList<IValueConverterHandler> Default { get; } =
    [
        ValueConverterHandler.CreateTo<string>(
            (val, _) => JsonSerializer.Serialize(val), 
            type => !SqliteTypeHelper.KnownTypes.Contains(type)
        )
    ];
}