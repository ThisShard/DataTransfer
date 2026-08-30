using Npgsql;
using ThisShard.Database.Core.Extensions;
using ThisShard.Database.Core.Models;
using ThisShard.Database.Core.Models.Descriptors;
using ThisShard.Database.Core.Models.Rows;
using ThisShard.Database.Core.Options;
using ThisShard.Database.Core.Readers;
using ThisShard.Database.Core.Writers;
using ThisShard.Database.Infrastructure.Postgres.Models;
using ThisShard.Database.Infrastructure.Postgres.Options;
using ThisShard.Database.Infrastructure.Postgres.Writers;

namespace ThisShard.Database.Infrastructure.Extensions;

/// <summary>
/// Расширения Npgsql для массовой записи строк
/// </summary>
public static class NpgsqlBulkOperationsExtensions
{
    #region BatchWrite

    /// <summary>
    /// Произвести запись строк с использованием батчинка
    /// </summary>
    public static async ValueTask BatchWrite(this NpgsqlConnection connection, string[] tablePath, Func<IRowWriter, ValueTask> writing, NpgsqlBulkOperationsOptions? options = null)
    {
        await connection.Write(
            cn => GetBatchWriter(cn, tablePath, options),
            writing
        );
    }
    
    /// <summary>
    /// Произвести запись строк с использованием батчинка
    /// </summary>
    public static async ValueTask BatchWrite(this NpgsqlConnection connection, PgTable table, Func<IRowWriter, ValueTask> writing, NpgsqlBulkOperationsOptions? options = null)
    {
        await connection.Write(
            cn => cn.GetBatchWriter(table, options),
            writing
        );
    }
    
    /// <summary>
    /// Произвести запись строк с использованием батчинка
    /// </summary>
    public static async ValueTask BatchWrite(this NpgsqlConnection connection, PgStagingTable table, Func<IRowWriter, ValueTask> writing, NpgsqlBulkOperationsOptions? options = null)
    {
        await connection.Write(
            cn => cn.GetBatchWriter(table, options),
            writing
        );
    }

    #endregion
    
    #region BulkWrite
    
    /// <summary>
    /// Произвести запись строк с использованием Bulk операций
    /// </summary>
    public static async ValueTask BulkWrite(this NpgsqlConnection connection, string[] tablePath, Func<IRowWriter, ValueTask> writing, NpgsqlBulkOperationsOptions? options = null)
    {
        await connection.Write(
            cn => cn.GetBulkWriter(tablePath, options),
            writing
        );
    }

    /// <summary>
    /// Произвести запись строк с использованием Bulk операций
    /// </summary>
    public static async ValueTask BulkWrite(this NpgsqlConnection connection, PgTable table, Func<IRowWriter, ValueTask> writing, NpgsqlBulkOperationsOptions? options = null)
    {
        await connection.Write(
            cn => cn.GetBulkWriter(table, options),
            writing
        );
    }

    /// <summary>
    /// Произвести запись строк с использованием Bulk операций
    /// </summary>
    public static async ValueTask BulkWrite(this NpgsqlConnection connection, PgStagingTable table, Func<IRowWriter, ValueTask> writing, NpgsqlBulkOperationsOptions? options = null)
    {
        await connection.Write(
            cn => cn.GetBulkWriter(table, options),
            writing
        );
    }

    #endregion
    
    #region GetBatchWriter
    
    /// <summary>
    /// Возвращает писателя для Batch операций
    /// </summary>
    public static async ValueTask<ITableWriter> GetBatchWriter(this NpgsqlConnection connection, string[] tablePath, NpgsqlBulkOperationsOptions? options = null)
    {
        options ??= NpgsqlBulkOperationsOptions.Default;

        var table = await options.TableManager.GetTable(connection, tablePath);
        if (table == null)
            throw new InvalidOperationException("Table not exists");

        var writer = CreateTableWriter(connection, cn => new PgBatchTableWriter(cn, options.BatchBufferSize, options.CommandFilterFactory), options);
        try
        {
            await writer.Init(table);
        }
        catch (Exception)
        {
            await writer.DisposeAsync();
            throw;
        }
        
        return writer;
    }
    
    /// <summary>
    /// Возвращает писателя для Batch операций
    /// </summary>
    public static async ValueTask<ITableWriter> GetBatchWriter(this NpgsqlConnection connection, PgTable table, NpgsqlBulkOperationsOptions? options = null)
    {
        options ??= NpgsqlBulkOperationsOptions.Default;

        var writer = CreateTableWriter(connection, cn => new PgBatchTableWriter(cn, options.BatchBufferSize, options.CommandFilterFactory), options);
        try
        {
            await writer.Init(table);
        }
        catch (Exception)
        {
            await writer.DisposeAsync();
            throw;
        }
        
        return writer;
    }
    
    /// <summary>
    /// Возвращает писателя для Batch операций
    /// </summary>
    public static async ValueTask<ITableWriter> GetBatchWriter(this NpgsqlConnection connection, PgStagingTable table, NpgsqlBulkOperationsOptions? options = null)
    {
        options ??= NpgsqlBulkOperationsOptions.Default;

        var writer = CreateTableWriter(connection, cn => new PgBatchTableWriter(cn, options.BatchBufferSize, options.CommandFilterFactory), options);
        try
        {
            await writer.Init(table);
        }
        catch (Exception)
        {
            await writer.DisposeAsync();
            throw;
        }
        
        return writer;
    }
    #endregion
    
    #region GetBulkWriter
    
    /// <summary>
    /// Возвращает писателя для Bulk операций
    /// </summary>
    public static async ValueTask<ITableWriter> GetBulkWriter(this NpgsqlConnection connection, string[] tablePath, NpgsqlBulkOperationsOptions? options = null)
    {
        options ??= NpgsqlBulkOperationsOptions.Default;

        var table = await options.TableManager.GetTable(connection, tablePath);
        if (table == null)
            throw new InvalidOperationException("Table not exists");

        var writer = CreateTableWriter(connection, cn =>
        {
            var descriptors = GetBulkDescriptors(cn, options);
            var bufferSize = GetMaxBufferSize(options);
            return new CompositeTableWriter(descriptors, bufferSize);
        }, options);
        
        try
        {
            await writer.Init(table);
        }
        catch (Exception)
        {
            await writer.DisposeAsync();
            throw;
        }
        
        return writer;
    }
    
    /// <summary>
    /// Возвращает писателя для Bulk операций
    /// </summary>
    public static async ValueTask<ITableWriter> GetBulkWriter(this NpgsqlConnection connection, PgTable table, NpgsqlBulkOperationsOptions? options = null)
    {
        options ??= NpgsqlBulkOperationsOptions.Default;

        var writer = CreateTableWriter(connection, cn =>
        {
            var descriptors = GetBulkDescriptors(cn, options);
            var bufferSize = GetMaxBufferSize(options);
            return new CompositeTableWriter(descriptors, bufferSize);
        }, options);
        
        try
        {
            await writer.Init(table);
        }
        catch (Exception)
        {
            await writer.DisposeAsync();
            throw;
        }
        
        return writer;
    }
    
    /// <summary>
    /// Возвращает писателя для Bulk операций
    /// </summary>
    public static async ValueTask<ITableWriter> GetBulkWriter(this NpgsqlConnection connection, PgStagingTable table, NpgsqlBulkOperationsOptions? options = null)
    {
        options ??= NpgsqlBulkOperationsOptions.Default;

        var writer = CreateTableWriter(connection, cn =>
        {
            var descriptors = GetBulkDescriptors(cn, options);
            var bufferSize = GetMaxBufferSize(options);
            return new CompositeTableWriter(descriptors, bufferSize);
        }, options);
        
        try
        {
            await writer.Init(table);
        }
        catch (Exception)
        {
            await writer.DisposeAsync();
            throw;
        }
        
        return writer;
    }
    #endregion
    
    #region GetTableInfo
    
    /// <summary>
    /// Возвращает объект таблицы по указанному пути
    /// </summary>
    public static async ValueTask<PgTable?> GetTableInfo(this NpgsqlConnection connection, string[] tablePath, NpgsqlBulkOperationsOptions? options = null)
    {
        options ??= NpgsqlBulkOperationsOptions.Default;
        return await options.TableManager.GetTable(connection, tablePath);
    }
    
    #endregion
    
    #region GetSustainableRowReader

    /// <summary>
    /// Возвращает читателя
    /// </summary>
    public static async ValueTask<IRowReader> GetSustainableRowReader(this NpgsqlConnection connection,
        string[] tablePath,
        RowState rowState = RowState.Added,
        NpgsqlBulkOperationsOptions? options = null,
        bool ownsConnection = false,
        IRow? startRow = null)
    {
        var table = await connection.GetTableInfo(tablePath, options);
        if (table == null)
            throw new InvalidOperationException("Table not exists");
        
        return connection.GetSustainableRowReader(table, rowState, options, ownsConnection, startRow);
    }
    
    /// <summary>
    /// Возвращает читателя
    /// </summary>
    public static async ValueTask<IRowReader> GetSustainableRowReader(this NpgsqlConnection connection,
        string[] tablePath,
        Func<NpgsqlConnection, NpgsqlCommand> commandFactory,
        RowState rowState = RowState.Added,
        NpgsqlBulkOperationsOptions? options = null,
        bool ownsConnection = false,
        IRow? startRow = null)
    {
        var table = await connection.GetTableInfo(tablePath, options);
        if (table == null)
            throw new InvalidOperationException("Table not exists");
        
        return connection.GetSustainableRowReader(table, commandFactory, rowState, options, ownsConnection, startRow);
    }
    
    /// <summary>
    /// Возвращает читателя
    /// </summary>
    public static IRowReader GetSustainableRowReader(this NpgsqlConnection connection,
        PgTable table,
        RowState rowState = RowState.Added,
        NpgsqlBulkOperationsOptions? options = null,
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
    public static IRowReader GetSustainableRowReader(this NpgsqlConnection connection,
        PgTable table,
        Func<NpgsqlConnection, NpgsqlCommand> commandFactory,
        RowState rowState = RowState.Added,
        NpgsqlBulkOperationsOptions? options = null,
        bool ownsConnection = false,
        IRow? startRow = null)
    {
        var primaryKey = table.Columns
            .Where(x => x.IsPrimaryKey)
            .OrderBy(x=>x.PrimaryKeyOrdinal)
            .ToArray();
        if (primaryKey.Length == 0)
            throw new InvalidOperationException("No primary key defined");
        
        options ??= NpgsqlBulkOperationsOptions.Default;
        return connection.GetSustainableRowReader(async (cn, row, writer, ct) =>
        {
            await using var command = commandFactory(cn);
            AdjustCommand(command, row ?? startRow, primaryKey);
            await using var reader = await command.ExecuteReaderAsync(ct).GetRowReader(rowState);
            await reader.WriteTo(writer, ct);
        }, options.SustainableOptions ?? SustainableOperationsOptions<NpgsqlConnection>.Disabled, ownsConnection);
    }

    /// <summary>
    /// Правит команду так, чтобы данные шли после указанной строки
    /// </summary>
    private static void AdjustCommand(NpgsqlCommand command, IRow? row, IReadOnlyCollection<PgColumn> primaryKey)
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
                var parameterName = $"@__PK__{parameterIndex++}";
                command.Parameters.Add(new NpgsqlParameter(parameterName, value));
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
    
    #region Private

    /// <summary>
    /// Создает писателья таблиц
    /// </summary>
    private static ITableWriter CreateTableWriter(NpgsqlConnection connection, Func<NpgsqlConnection, ITableWriter> factory, NpgsqlBulkOperationsOptions options)
    {
        var writer = options.SustainableOptions == null 
            ? factory(connection) 
            : connection.GetSustainableTableWriter(factory, options.SustainableOptions);

        if (options.ValueConverter != null)
        {
            writer = writer.UsingValueConverter(options.ValueConverter!);
        }
        
        return writer;
    }
    
    /// <summary>
    /// Возвращает максимальный размер буфера из настроек
    /// </summary>
    private static int GetMaxBufferSize(NpgsqlBulkOperationsOptions options)
    {
        return options.BulkBufferSize > options.BatchBufferSize 
            ? options.BulkBufferSize 
            : options.BatchBufferSize;
    }

    /// <summary>
    /// Возвращает дескрипторы для Bulk
    /// </summary>
    private static TableWriterDescriptor[] GetBulkDescriptors(NpgsqlConnection connection,
        NpgsqlBulkOperationsOptions options)
    {
        return 
        [
            new()
            {
                MinRows = options.BulkMinRows,
                Owned = true,
                Writer = new PgBulkTableWriter(connection, options.StagingTableManager, options.BulkBufferSize, options.CommandFilterFactory)
            },
            new()
            {
                Owned = true,
                Writer = new PgBatchTableWriter(connection, options.BatchBufferSize, options.CommandFilterFactory)
            },
        ];
    }
    #endregion
}