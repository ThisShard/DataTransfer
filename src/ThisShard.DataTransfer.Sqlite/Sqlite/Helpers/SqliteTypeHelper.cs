using Microsoft.Data.Sqlite;

namespace ThisShard.Database.Infrastructure.Sqlite.Helpers;

internal static class SqliteTypeHelper
{
    /// <summary>
    /// Мэппинги типов Sqlite
    /// </summary>
    private static readonly Dictionary<Type, SqliteType> SqliteTypeMapping =
        new()
        {
            { typeof(bool), SqliteType.Integer },
            { typeof(byte), SqliteType.Integer },
            { typeof(byte[]), SqliteType.Blob },
            { typeof(char), SqliteType.Text },
            { typeof(DateTime), SqliteType.Text },
            { typeof(DateTimeOffset), SqliteType.Text },
            { typeof(DateOnly), SqliteType.Text },
            { typeof(TimeOnly), SqliteType.Text },
            { typeof(DBNull), SqliteType.Text },
            { typeof(decimal), SqliteType.Text },
            { typeof(double), SqliteType.Real },
            { typeof(float), SqliteType.Real },
            { typeof(Guid), SqliteType.Text },
            { typeof(int), SqliteType.Integer },
            { typeof(long), SqliteType.Integer },
            { typeof(sbyte), SqliteType.Integer },
            { typeof(short), SqliteType.Integer },
            { typeof(string), SqliteType.Text },
            { typeof(TimeSpan), SqliteType.Text },
            { typeof(uint), SqliteType.Integer },
            { typeof(ulong), SqliteType.Integer },
            { typeof(ushort), SqliteType.Integer }
        };

    private static readonly Dictionary<SqliteType, Type> TypeMapping =
        new()
        {
            { SqliteType.Blob, typeof(byte[]) },
            { SqliteType.Text, typeof(string) },
            { SqliteType.Real, typeof(double) },
            { SqliteType.Integer, typeof(long) }
        };
    
    /// <summary>
    /// Известные типы у Sqlite
    /// </summary>
    public static IReadOnlyCollection<Type> KnownTypes => SqliteTypeMapping.Keys;
    
    /// <summary>
    /// Возвращает тип Sqlite
    /// </summary>
    public static SqliteType GetSqliteType(this Type type) => SqliteTypeMapping.GetValueOrDefault(type, SqliteType.Text);

    /// <summary>
    /// Возвращает тип для SqliteType
    /// </summary>
    public static Type AsType(this SqliteType type) => TypeMapping[type];
    
    /// <summary>
    /// Возвращает строковое представление типа Sqlite
    /// </summary>
    public static string GetSqliteTypeString(this Type type) => GetSqliteType(type).ToString();
}