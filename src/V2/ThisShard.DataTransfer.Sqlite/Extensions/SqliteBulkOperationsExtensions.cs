using Microsoft.Data.Sqlite;
using ThisShard.Database.Models;
using ThisShard.Database.Models.Rows;
using ThisShard.Database.Models.Tables;
using ThisShard.Database.Options;
using ThisShard.Database.Readers;
using ThisShard.Database.Writers;

namespace ThisShard.Database.Extensions;

/// <summary>
/// Расширения Sqlite для массовой записи строк
/// </summary>
public static class SqliteBulkOperationsExtensions
{
    #region Write

    /// <summary>
    /// Произвести запись строк
    /// </summary>
    public static async ValueTask Write(this SqliteConnection connection, string tableName, Func<IWriter, ValueTask> writing, SqliteBulkOperationsOptions? options = null) 
    {
        options ??= SqliteBulkOperationsOptions.Default;
        await connection.Write(
            cn => GetWriter(cn, tableName, options),
            writing
        );
    }
    
    /// <summary>
    /// Произвести запись строк
    /// </summary>
    public static async ValueTask Write(this SqliteConnection connection, SqliteTable table, Func<IWriter, ValueTask> writing, SqliteBulkOperationsOptions? options = null)
    {
        options ??= SqliteBulkOperationsOptions.Default;
        await connection.Write(
            cn => cn.GetWriter(table, options),
            writing
        );
    }
    
    /// <summary>
    /// Создать таблицу и произвести запись строк
    /// </summary>
    public static async ValueTask CreateTableAndWrite(this SqliteConnection connection, ITable table, Func<IWriter, ValueTask> writing, SqliteBulkOperationsOptions? options = null)
    {
        options ??= SqliteBulkOperationsOptions.Default;
        await connection.Write(
            cn => cn.CreateTableAndGetWriter(table, options),
            writing
        );
    }
    
    #endregion
    
    #region GetWriter
    
    /// <summary>
    /// Возвращает писателя
    /// </summary>
    public static async ValueTask<IWriter> GetWriter(this SqliteConnection connection, string tableName, SqliteBulkOperationsOptions? options = null)
    {
        options ??= SqliteBulkOperationsOptions.Default;

        var table = await options.TableManager.GetTable(connection, tableName);
        if (table == null)
            throw new InvalidOperationException("Table not exists");

        return await connection.GetWriter(table, options);
    }
    
    /// <summary>
    /// Возвращает писателя
    /// </summary>
    public static async ValueTask<IWriter> GetWriter(this SqliteConnection connection, SqliteTable table, SqliteBulkOperationsOptions? options = null)
    {
        options ??= SqliteBulkOperationsOptions.Default;

        IWriter writer = new SqliteTableWriter(connection, table, options.BatchBufferSize, options.CommandFilterFactory);
        try
        {
            await writer.Init();
        }
        catch (Exception)
        {
            await writer.DisposeAsync();
            throw;
        }
        
        if (options.ValueConverter != null)
            writer = writer.UsingValueConverter(table, options.ValueConverter);

        return writer;
    }
    
    /// <summary>
    /// Создает таблицу и возвращает писателя для Batch операций
    /// </summary>
    public static async ValueTask<IWriter> CreateTableAndGetWriter(this SqliteConnection connection, ITable table, SqliteBulkOperationsOptions? options = null) => 
        await GetWriter(connection, await connection.CreateTable(table, options), options);

    
    /// <summary>
    /// Создает таблицу и возвращает писателя для Batch операций
    /// </summary>
    public static async ValueTask<IWriter> CreateTableAndGetWriter(this SqliteConnection connection, ITable table, string name, SqliteBulkOperationsOptions? options = null) => 
        await GetWriter(connection, await connection.CreateTable(table, name, options), options);

    #endregion
    
    #region GetSustainableReader

    /// <summary>
    /// Возвращает читателя
    /// </summary>
    public static async ValueTask<IReader> GetSustainableReader(this SqliteConnection connection,
        string tableName,
        RowState rowState = RowState.Added,
        SqliteBulkOperationsOptions? options = null,
        bool ownsConnection = false,
        IRow? startRow = null)
    {
        var table = await connection.GetTableInfo(tableName, options);
        if (table == null)
            throw new InvalidOperationException("Table not exists");
        
        return connection.GetSustainableReader(table, rowState, options, ownsConnection, startRow);
    }
    
    /// <summary>
    /// Возвращает читателя
    /// </summary>
    public static async ValueTask<IReader> GetSustainableReader(this SqliteConnection connection,
        string tableName,
        Func<SqliteConnection, SqliteCommand> commandFactory,
        RowState rowState = RowState.Added,
        SqliteBulkOperationsOptions? options = null,
        bool ownsConnection = false,
        IRow? startRow = null)
    {
        var table = await connection.GetTableInfo(tableName, options);
        if (table == null)
            throw new InvalidOperationException("Table not exists");
        
        return connection.GetSustainableReader(table, commandFactory, rowState, options, ownsConnection, startRow);
    }
    
    /// <summary>
    /// Возвращает читателя
    /// </summary>
    public static IReader GetSustainableReader(this SqliteConnection connection,
        SqliteTable table,
        RowState rowState = RowState.Added,
        SqliteBulkOperationsOptions? options = null,
        bool ownsConnection = false,
        IRow? startRow = null) => connection.GetSustainableReader(table, cn =>
    {
        var command = cn.CreateCommand();
        command.CommandText = $"SELECT * FROM {table.Path}";
        return command;
    }, rowState, options, ownsConnection, startRow);
    
    /// <summary>
    /// Возвращает читателя
    /// </summary>
    public static IReader GetSustainableReader(this SqliteConnection connection,
        SqliteTable table,
        Func<SqliteConnection, SqliteCommand> commandFactory,
        RowState rowState = RowState.Added,
        SqliteBulkOperationsOptions? options = null,
        bool ownsConnection = false,
        IRow? startRow = null)
    {
        var primaryKey = table.Columns
            .Where(x => x.IsPrimaryKey)
            .OrderBy(x=>x.PrimaryKeyOrdinal)
            .ToArray();
        if (primaryKey.Length == 0)
            throw new InvalidOperationException("No primary key defined");
        
        options ??= SqliteBulkOperationsOptions.Default;
        return connection.GetSustainableReader(async (cn, row, writer, ct) =>
        {
            await using var command = commandFactory(cn);
            AdjustCommand(command, row ?? startRow, primaryKey);
            await using var reader = await command.ExecuteReaderAsync(ct).GetReader(rowState);
            await reader.WriteTo(writer, ct);
        }, options.SustainableOptions ?? SustainableOperationsOptions<SqliteConnection>.Disabled, ownsConnection);
    }

    /// <summary>
    /// Правит команду так, чтобы данные шли после указанной строки
    /// </summary>
    private static void AdjustCommand(SqliteCommand command, IRow? row, IReadOnlyCollection<SqliteColumn> primaryKey)
    {
        var orderByPrimaryKey = $" ORDER BY {string.Join(", ", primaryKey.Select(x => $"t.{x.Path}"))}";

        var where = "";
        if (row != null)
        {
            var clauses = new List<string>();
            var equalsClauses = new List<string>();
            var parameterIndex = 0;
            foreach (var primaryKeyColumn in primaryKey)
            {
                row.TryGetValue(primaryKeyColumn.Key, out var value);
                var parameterName = $"$__PK__{parameterIndex++}";
                command.Parameters.Add(new SqliteParameter(parameterName, value));
                clauses.Add(string.Join(" AND ", [..equalsClauses, $"t.{primaryKeyColumn.Path} > {parameterName}"]));
                equalsClauses.Add($"t.{primaryKeyColumn.Path} = {parameterName}");
            }

            where = clauses.Count == 1 
                ? $" WHERE {clauses[0]}" 
                : $" WHERE {string.Join(" OR ", clauses.Select(c=>$"({c})"))}";
        }
        
        command.CommandText = $"SELECT * FROM ({command.CommandText}) t{where}{orderByPrimaryKey}";
    }

    #endregion
    
    #region CreateTable
    
    /// <summary>
    /// Создает таблицу
    /// </summary>
    public static async Task<SqliteTable> CreateTable(this SqliteConnection connection, ITable table, SqliteBulkOperationsOptions? options = null)
    {
        options ??= SqliteBulkOperationsOptions.Default;

        var convertedTable = options.TableManager.ConvertTable(table);
        
        await options.TableManager.CreateTable(connection, convertedTable);
        
        return convertedTable;
    }
    
    /// <summary>
    /// Создает таблицу с модификацией
    /// </summary>
    public static async Task<SqliteTable> CreateTable(this SqliteConnection connection, ITable table, string name, SqliteBulkOperationsOptions? options = null)
    {
        options ??= SqliteBulkOperationsOptions.Default;

        var convertedTable = options.TableManager.ConvertTable(table, name);
        
        await options.TableManager.CreateTable(connection, convertedTable);
        
        return convertedTable;
    }
    
    #endregion
    
    #region GetTableInfo
    
    /// <summary>
    /// Возвращает объект таблицы по указанному пути
    /// </summary>
    public static async ValueTask<SqliteTable?> GetTableInfo(this SqliteConnection connection, string tableName, SqliteBulkOperationsOptions? options = null)
    {
        options ??= SqliteBulkOperationsOptions.Default;
        return await options.TableManager.GetTable(connection, tableName);
    }
    
    #endregion
}