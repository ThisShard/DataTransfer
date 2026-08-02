using ThisShard.Database.Infrastructure.Json.Helpers;

namespace ThisShard.Database.Infrastructure.Json.Tables;

/// <summary>
/// Резолверы имен свойств
/// </summary>
public static class JsonPropertyNameResolvers
{
    /// <summary>
    /// Резолвер имени свойства без конверсии
    /// </summary>
    public static Func<string, string> NonConvertingPropertyNameResolver { get; set; } = name => name;

    /// <summary>
    /// Резолвер имени свойства с конверсией в CamelCase
    /// </summary>
    public static Func<string, string> CamelCasePropertyNameResolver { get; set; } = JsonNamingHelper.ToCamelCaseWithAcronyms;
}