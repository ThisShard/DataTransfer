using Microsoft.Data.Sqlite;
using ThisShard.Database.Core.Extensions;
using ThisShard.Database.Core.Models;
using ThisShard.Database.Core.Models.Rows;
using ThisShard.Database.Core.Models.Tables;
using ThisShard.Database.Core.Options;
using ThisShard.Database.Core.Readers;
using ThisShard.Database.Core.Writers;
using ThisShard.Database.Infrastructure.Sqlite.Models;
using ThisShard.Database.Infrastructure.Sqlite.Options;
using ThisShard.Database.Infrastructure.Sqlite.Writers;

namespace ThisShard.Database.Infrastructure.Extensions;

/// <summary>
/// Расширения Sqlite для массовой записи строк
/// </summary>
public static class SqliteBulkOperationsExtensions
{
    #region Write

    /// <summary>
    /// Произвести запись строк
    /// </summary>
    public static async ValueTask Write(this SqliteConnection connection, string tableName, Func<IRowWriter, ValueTask> writing, SqliteBulkOperationsOptions? options = null) 
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
    public static async ValueTask Write(this SqliteConnection connection, SqliteTable table, Func<IRowWriter, ValueTask> writing, SqliteBulkOperationsOptions? options = null)
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
    public static async ValueTask CreateTableAndWrite(this SqliteConnection connection, ITable table, Func<IRowWriter, ValueTask> writing, SqliteBulkOperationsOptions? options = null)
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
    public static async ValueTask<ITableWriter> GetWriter(this SqliteConnection connection, string tableName, SqliteBulkOperationsOptions? options = null)
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
    public static async ValueTask<ITableWriter> GetWriter(this SqliteConnection connection, SqliteTable table, SqliteBulkOperationsOptions? options = null)
    {
        options ??= SqliteBulkOperationsOptions.Default;

        ITableWriter writer = new SqliteTableWriter(connection, options.BatchBufferSize, options.CommandFilterFactory);
        try
        {
            await writer.Init(table);
        }
        catch (Exception)
        {
            await writer.DisposeAsync();
            throw;
        }
        
        if (options.ValueConverter != null)
            writer = writer.UsingValueConverter(options.ValueConverter);

        return writer;
    }
    
    /// <summary>
    /// Создает таблицу и возвращает писателя для Batch операций
    /// </summary>
    public static async ValueTask<ITableWriter> CreateTableAndGetWriter(this SqliteConnection connection, ITable table, SqliteBulkOperationsOptions? options = null) => 
        await GetWriter(connection, await connection.CreateTable(table, options), options);

    #endregion
    
    #region GetSustainableRowReader

    /// <summary>
    /// Возвращает читателя
    /// </summary>
    public static async ValueTask<IRowReader> GetSustainableRowReader(this SqliteConnection connection,
        string tableName,
        RowState rowState = RowState.Added,
        SqliteBulkOperationsOptions? options = null,
        bool ownsConnection = false,
        IRow? startRow = null)
    {
        var table = await connection.GetTableInfo(tableName, options);
        if (table == null)
            throw new InvalidOperationException("Table not exists");
        
        return connection.GetSustainableRowReader(table, rowState, options, ownsConnection, startRow);
    }
    
    /// <summary>
    /// Возвращает читателя
    /// </summary>
    public static async ValueTask<IRowReader> GetSustainableRowReader(this SqliteConnection connection,
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
        
        return connection.GetSustainableRowReader(table, commandFactory, rowState, options, ownsConnection, startRow);
    }
    
    /// <summary>
    /// Возвращает читателя
    /// </summary>
    public static IRowReader GetSustainableRowReader(this SqliteConnection connection,
        SqliteTable table,
        RowState rowState = RowState.Added,
        SqliteBulkOperationsOptions? options = null,
        bool ownsConnection = false,
        IRow? startRow = null) => connection.GetSustainableRowReader(table, cn =>
    {
        var command = cn.CreateCommand();
        command.CommandText = $"SELECT * FROM {table.Path}";
        return command;
    }, rowState, options, ownsConnection, startRow);
    
    /// <summary>
    /// Возвращает читателя
    /// </summary>
    public static IRowReader GetSustainableRowReader(this SqliteConnection connection,
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
        return connection.GetSustainableRowReader(async (cn, row, writer, ct) =>
        {
            await using var command = commandFactory(cn);
            AdjustCommand(command, row ?? startRow, primaryKey);
            await using var reader = await command.ExecuteReaderAsync(ct).GetRowReader(rowState);
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