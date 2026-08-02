using System.Data;
using Npgsql;
using ThisShard.Database.Infrastructure.Postgres.Helpers;
using ThisShard.Database.Infrastructure.Postgres.Models;

namespace ThisShard.Database.Infrastructure.Postgres.Tables;

/// <summary>
/// Менеджер таблиц постгреса
/// </summary>
public class PgTableManager : IPgTableManager
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
    
    public PgTableManager() : this(DefaultTableKeyResolver, DefaultColumnKeyResolver, DefaultPrimaryKeyNameResolver)
    {
    }
    
    public PgTableManager(Func<string, string> tableKeyResolver, Func<string, string, string> columnKeyResolver, Func<string, string> primaryKeyNameResolver)
    {
        _tableKeyResolver = tableKeyResolver ?? throw new ArgumentNullException(nameof(tableKeyResolver));
        _columnKeyResolver = columnKeyResolver ?? throw new ArgumentNullException(nameof(columnKeyResolver));
        _primaryKeyNameResolver = primaryKeyNameResolver ?? throw new ArgumentNullException(nameof(primaryKeyNameResolver));
    }
    
    #region GetTable

    /// <summary>
    /// Возвращает схему таблицы для указанного пути
    /// </summary>
    public async Task<PgTable?> GetTable(NpgsqlConnection connection, params string[] path)
    {
        var pgPath = PostgresNameFormatter.EscapePath(path);
        
        var schemaTable = await GetSchemaTable(connection, pgPath);
        if (schemaTable == null)
            return null;

        var (schema, tableName) = PostgresNameFormatter.ParseTablePath(path);
        var primaryKey = await GetPrimaryKey(connection, pgPath);
        var tableKey = _tableKeyResolver(string.Join(".", path));
        
        var columns = GetColumns(schemaTable, primaryKey, tableKey).ToList();
        await CorrectArrayColumnTypes(connection, columns, pgPath);
        await CorrectGeneratedColumns(connection, columns, schema, tableName!);

        return new PgTable
        {
            Key = tableKey,
            Path = pgPath,
            RawPath = path,
            Columns = columns,
        };
    }

    /// <summary>
    /// Получает схему таблицы из БД
    /// </summary>
    private async Task<DataTable?> GetSchemaTable(NpgsqlConnection connection, string path, string columnsSql = "*")
    {
        await using var command = connection.CreateCommand();

        command.CommandText = $"SELECT {columnsSql} FROM {path} LIMIT 0";

        await using var reader = await command.ExecuteReaderAsync(CommandBehavior.KeyInfo | CommandBehavior.SchemaOnly);
        var schemaTable = await reader.GetSchemaTableAsync();

        if (schemaTable == null)
            return null;
        
        return schemaTable;
    }

    /// <summary>
    /// Получает первичный ключ из БД
    /// </summary>
    private async Task<IReadOnlyDictionary<string, (int Index, short Options)>?> GetPrimaryKey(NpgsqlConnection connection, string path)
    {
        await using var command = connection.CreateCommand();

        command.CommandText = $"""
                               select t.attname, indoption 
                               from (
                               	SELECT unnest(indkey) indkey, unnest(indoption) indoption, indrelid
                               	FROM pg_index 
                               	WHERE indrelid = '{path}'::regclass AND indisprimary = true
                               ) i
                               inner join pg_attribute t on t.attrelid=i.indrelid AND t.attnum=i.indkey
                               """;

        
        await using var reader = await command.ExecuteReaderAsync();
        
        var result = new Dictionary<string, (int, short)>();
        var index = 0;
        while (await reader.ReadAsync())
        {
            var attribute = reader.GetFieldValue<string>(0);
            var options = reader.GetFieldValue<short>(1);
            result[attribute] = (index++, options);
        }
        
        return result;
    }
    
    /// <summary>
    /// Получает генерируемые колонки из БД
    /// </summary>
    private async Task<IReadOnlyCollection<string>> GetGeneratedColumns(NpgsqlConnection connection, string? schema, string tableName)
    {
        await using var command = connection.CreateCommand();

        command.CommandText = $"""
                               SELECT column_name 
                               FROM information_schema.columns 
                               WHERE table_schema = @schema AND table_name = @name AND is_generated = 'ALWAYS';
                               """;

        command.Parameters.Add(new NpgsqlParameter("schema", schema ?? "public"));
        command.Parameters.Add(new NpgsqlParameter("name", tableName));
        
        await using var reader = await command.ExecuteReaderAsync();

        var result = new HashSet<string>();
        while (await reader.ReadAsync())
        {
            var attribute = reader.GetFieldValue<string>(0);
            result.Add(attribute);
        }
        
        return result;
    }

    /// <summary>
    /// Возвращает колонки из таблицы схемы
    /// </summary>
    private IEnumerable<PgColumn> GetColumns(DataTable schemaTable,
        IReadOnlyDictionary<string, (int Index, short Options)>? primaryKey, string tableKey)
    {
        foreach (DataRow row in schemaTable.Rows)
        {
            var name = row.Field<string>("ColumnName")!;
            var dataTypeName = row.Field<string>("DataTypeName")!;
            var type = row.Field<Type>("DataType")!;
            var isKey = row.Field<bool>("IsKey");
            var isReadOnly = row.Field<bool>("IsReadOnly");
            var isIdentity = row.Field<bool>("IsIdentity");
            var isAutoIncrement = row.Field<bool>("IsAutoIncrement");
            var isAliased = row.Field<bool>("IsAliased");
            var isExpression = row.Field<bool>("IsExpression");
            var isRowVersion = row.Field<bool>("IsRowVersion");
            var isNullable = row.Field<bool?>("AllowDBNull");
            
            var primaryKeyOptions = isKey && primaryKey != null
                ? primaryKey[name]
                : ((int Index, short Options)?)null;
            
            var key = _columnKeyResolver(tableKey, name);
            
            yield return new PgColumn()
            {
                Key = key,
                Path = PostgresNameFormatter.EscapePath(name),
                RawName = name,
                IsPrimaryKey = isKey,
                IsReadOnly = isReadOnly || isIdentity || isAutoIncrement || isAliased || isExpression || isRowVersion,
                DataTypeName = dataTypeName,
                Type = type,
                PrimaryKeyOrdinal = primaryKeyOptions?.Index,
                PrimaryKeyDesc = primaryKeyOptions != null ? (primaryKeyOptions.Value.Options & 1) == 1 : null,
                PrimaryKeyNullsFirst = primaryKeyOptions != null ? (primaryKeyOptions.Value.Options & 2) == 2 : null,
                IsNullable = isNullable ?? true,
            };
        }
    }

    /// <summary>
    /// Корректирует типы у колонок массивов
    /// </summary>
    private async Task CorrectArrayColumnTypes(NpgsqlConnection connection, List<PgColumn> columns, string pgPath)
    {
        var arrayColumns = columns
            .Where(x => x.Type == typeof(Array))
            .Select(x=>new
            {
                Column = x,
                Dimensions = x.DataTypeName.Count(c => c == '[')
            })
            .ToArray();
        if (!arrayColumns.Any())
            return;

        var columnsSql = string.Join(", ",
            arrayColumns.Select(x => $"{x.Column.Path}{string.Join("", Enumerable.Repeat("[0]", x.Dimensions))}"));
        var schemaTable = await GetSchemaTable(connection, pgPath, columnsSql);
        if (schemaTable == null)
            return;
        
        foreach (var (row, columnInfo) in schemaTable.Rows.OfType<DataRow>().Zip(arrayColumns, (row, columnInfo) => (row, columnInfo)))
        {
            var type = row.Field<Type>("DataType")!;
            columnInfo.Column.Type = type.MakeArrayType(columnInfo.Dimensions);
        }
    }

    /// <summary>
    /// Корректирует колонки, которые генерируемые
    /// </summary>
    private async Task CorrectGeneratedColumns(NpgsqlConnection connection, List<PgColumn> columns, string? schema, string tableName)
    {
        var generatedColumns = await GetGeneratedColumns(connection, schema, tableName);
        foreach (var column in columns)
        {
            if (generatedColumns.Contains(column.RawName))
                column.IsReadOnly = true;
        }
    }

    #endregion
    
    /// <summary>
    /// Создать таблицу
    /// </summary>
    public async Task CreateTable(NpgsqlConnection connection, PgTable table)
    {
        await using var command = connection.CreateCommand();

        var columnSqls = table.Columns.Select(col => $"{col.Path} {col.DataTypeName}").ToList();

        var pkSqls = table.Columns
            .Where(x => x.IsPrimaryKey)
            .OrderBy(x => x.PrimaryKeyOrdinal)
            .Select(x =>
                $"{x.Path} {(x.PrimaryKeyDesc == true ? "DESC" : "")} {(x.PrimaryKeyNullsFirst == true ? "NULLS FIRST" : "")}")
            .ToArray();

        if (pkSqls.Any())
            columnSqls.Add($"CONSTRAINT {PostgresNameFormatter.EscapePath(_primaryKeyNameResolver(table.RawPath.Last()))} PRIMARY KEY ({string.Join(", ", pkSqls)})");
        
        var columnsSql = string.Join(", ", columnSqls);
        
        
        command.CommandText = $"""
                               CREATE TABLE IF NOT EXISTS {table.Path} ({columnsSql});
                               """;
        
        await command.ExecuteNonQueryAsync();
    }

    /// <summary>
    /// Удалить таблицу
    /// </summary>
    public async Task DeleteTable(NpgsqlConnection connection, PgTable table)
    {
        await using var command = connection.CreateCommand();
        
        command.CommandText = $"""
                               DROP TABLE IF EXISTS {table.Path};
                               """;
        
        await command.ExecuteNonQueryAsync();
    }

}