namespace ThisShard.Database.Infrastructure.Sqlite.Helpers;

internal static class SqliteNameFormatter
{
    /// <summary>
    /// Возвращает безопасный путь для таблицы
    /// </summary>
    public static string EscapePath(string path) => $"\"{path.Replace("\"", "\"\"")}\"";
}