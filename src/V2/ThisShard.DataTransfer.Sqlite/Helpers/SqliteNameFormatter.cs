namespace ThisShard.Database.Helpers;

internal static class SqliteNameFormatter
{
    /// <summary>
    /// Возвращает безопасный путь для таблицы
    /// </summary>
    public static string EscapePath(string path) => $"\"{path.Replace("\"", "\"\"")}\"";
}