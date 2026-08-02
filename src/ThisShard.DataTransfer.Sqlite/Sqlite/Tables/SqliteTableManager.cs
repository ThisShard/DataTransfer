using System.Data;
using Microsoft.Data.Sqlite;
using ThisShard.Database.Core.Models;
using ThisShard.Database.Core.Models.Tables;
using ThisShard.Database.Infrastructure.Sqlite.Helpers;
using ThisShard.Database.Infrastructure.Sqlite.Models;

namespace ThisShard.Database.Infrastructure.Sqlite.Tables;

/// <summary>
/// Менеджер таблиц постгреса
/// </summary>
public class SqliteTableManager : ISqliteTableManager
{
    /// <summary>
    /// Резолвер ключа таблицы
    /// </summary>
    public static Func<string, string> DefaultTableKeyResolver { get; set; } = key => key;

    /// <summary>
    /// Резолвер ключа столбца по умолчанию
    /// </summary>
    public static Func<string, string, string> DefaultColumnKeyResolver { get; set; } = (_, key) => key;

    /// <summary>
    /// Резолвер имени первичного ключа по умолчанию
    /// </summary>
    public static Func<string, string> DefaultPrimaryKeyNameResolver { get; set; } =
        table => $"PK_{table}";
    
    private readonly Func<string, string> _tableKeyResolver;
    private readonly Func<string,string,string> _columnKeyResolver;
    private readonly Func<string, string> _primaryKeyNameResolver;
    
    public SqliteTableManager() : this(DefaultTableKeyResolver, DefaultColumnKeyResolver, DefaultPrimaryKeyNameResolver)
    {
    }
    
    public SqliteTableManager(Func<string, string> tableKeyResolver, Func<string, string, string> columnKeyResolver, Func<string, string> primaryKeyNameResolver)
    {
        _tableKeyResolver = tableKeyResolver ?? throw new ArgumentNullException(nameof(tableKeyResolver));
        _columnKeyResolver = columnKeyResolver ?? throw new ArgumentNullException(nameof(columnKeyResolver));
        _primaryKeyNameResolver = primaryKeyNameResolver ?? throw new ArgumentNullException(nameof(primaryKeyNameResolver));
    }

    #region GetTable

    /// <summary>
    /// Возвращает схему таблицы для указанного пути
    /// </summary>
    public async Task<SqliteTable?> GetTable(SqliteConnection connection, string name)
    {;
        var tablePath = SqliteNameFormatter.EscapePath(name);
        
        var schemaTable = await GetSchemaTable(connection, tablePath);
        if (schemaTable == null)
            return null;

        var tableKey = _tableKeyResolver(name);
        
        var columns = GetColumns(schemaTable, tableKey).ToList();

        return new SqliteTable
        {
            Key = tableKey,
            Path = tablePath,
            RawName = name,
            Columns = columns,
        };
    }

    /// <summary>
    /// Возвращает колонки из таблицы схемы
    /// </summary>
    private IEnumerable<SqliteColumn> GetColumns(DataTable schemaTable, string tableKey)
    {
        foreach (DataRow row in schemaTable.Rows)
        {
            var name = row.Field<string>("ColumnName")!;
            var type = row.Field<Type>("DataType")!;
            var isKey = row.Field<bool>("IsKey");
            var isAutoIncrement = row.Field<bool>("IsAutoIncrement");
            var isAliased = row.Field<bool>("IsAliased");
            var isExpression = row.Field<bool>("IsExpression");
            var isNullable = row.Field<bool?>("AllowDBNull");
            
            var key = _columnKeyResolver(tableKey, name);
            
            yield return new SqliteColumn()
            {
                Key = key,
                Path = SqliteNameFormatter.EscapePath(name),
                RawName = name,
                IsPrimaryKey = isKey,
                IsReadOnly = isAutoIncrement || isAliased || isExpression,
                DataTypeName = type.GetSqliteTypeString(),
                Type = type,
                IsNullable = isNullable ?? true,
            };
        }
    }

    /// <summary>
    /// Получает схему таблицы из БД
    /// </summary>
    private async Task<DataTable?> GetSchemaTable(SqliteConnection connection, string path)
    {
        await using var command = connection.CreateCommand();

        command.CommandText = $"SELECT * FROM {path} LIMIT 0";

        await using var reader = await command.ExecuteReaderAsync(CommandBehavior.KeyInfo | CommandBehavior.SchemaOnly);
        var schemaTable = await reader.GetSchemaTableAsync();

        if (schemaTable == null)
            return null;
        
        return schemaTable;
    }

    #endregion

    /// <summary>
    /// Создать таблицу
    /// </summary>
    public async Task CreateTable(SqliteConnection connection, SqliteTable table)
    {
        await using var command = connection.CreateCommand();

        var columnSqls = table.Columns.Select(col => $"{col.Path} {col.DataTypeName}").ToList();

        var pkSqls = table.Columns
            .Where(x => x.IsPrimaryKey)
            .OrderBy(x => x.PrimaryKeyOrdinal)
            .Select(x => x.Path)
            .ToArray();

        if (pkSqls.Any())
            columnSqls.Add($"CONSTRAINT {SqliteNameFormatter.EscapePath(_primaryKeyNameResolver(table.RawName))} PRIMARY KEY ({string.Join(", ", pkSqls)})");
        
        var columnsSql = string.Join(", ", columnSqls);
        
        
        command.CommandText = $"""
                               CREATE TABLE IF NOT EXISTS {table.Path} ({columnsSql});
                               """;
        
        await command.ExecuteNonQueryAsync();
    }

    /// <summary>
    /// Удалить таблицу
    /// </summary>
    public async Task DeleteTable(SqliteConnection connection, SqliteTable table)
    {
        await using var command = connection.CreateCommand();
        
        command.CommandText = $"""
                               DROP TABLE IF EXISTS {table.Path};
                               """;
        
        await command.ExecuteNonQueryAsync();
    }

    #region ConvertTable
    
    /// <summary>
    /// Конвертирует таблицу в Sqlite
    /// </summary>
    public SqliteTable ConvertTable(ITable table)
    {
        var columns = ConvertColumns(table).ToArray();
        var rawName = table.RawPath.Last();
        return new SqliteTable()
        {
            Key = table.Key,
            Columns = columns,
            Path = SqliteNameFormatter.EscapePath(rawName),
            RawName = rawName,
        };
    }

    /// <summary>
    /// Конвертирует колонки в Sqlite
    /// </summary>
    private IEnumerable<SqliteColumn> ConvertColumns(ITable table)
    {
        foreach (var column in table.Columns)
        {
            yield return new SqliteColumn
            {
                Key = column.Key,
                RawName = column.RawName,
                Path = SqliteNameFormatter.EscapePath(column.RawName),
                Type = column.Type.GetSqliteType().AsType(),
                DataTypeName = column.Type.GetSqliteTypeString(),
                IsPrimaryKey = column.IsPrimaryKey,
                IsNullable = column.IsNullable,
            };
        }
    }

    #endregion
}