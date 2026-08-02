namespace ThisShard.Database.Infrastructure.Postgres.Helpers;

internal static class PostgresNameFormatter
{
    public static (string? Schema, string? TableName) ParseTablePath(params string[] path)
    {
        if (path.Length == 0)
            return (null, null);
        
        if (path.Length == 1)
            return (null, path[0]);
        
        return (path[^1], path[^2]);
    }
    
    /// <summary>
    /// Возвращает безопасный путь с измененным именем
    /// </summary>
    public static string EscapePathAndReplaceName(IEnumerable<string> path, Func<string, string> replacement)
    {
        return EscapePath(ReplaceName(path, replacement));
    }

    /// <summary>
    /// Заменяет имя в пути
    /// </summary>
    public static string[] ReplaceName(IEnumerable<string> path, Func<string, string> replacement)
    {
        var pathArray = path.ToArray();
        if (pathArray.Length == 0)
            return pathArray;
        
        pathArray[^1] = replacement(pathArray[^1]);
        return pathArray;
    }
    
    /// <summary>
    /// Возвращает безопасный путь для таблицы
    /// </summary>
    public static string EscapePath(params string[] path) => EscapePath((IEnumerable<string>)path);

    /// <summary>
    /// Возвращает безопасный путь для таблицы
    /// </summary>
    public static string EscapePath(IEnumerable<string> path)
    {
        return string.Join(".", path.Select(x => $"\"{x.Replace("\"", "\"\"")}\""));
    }
}