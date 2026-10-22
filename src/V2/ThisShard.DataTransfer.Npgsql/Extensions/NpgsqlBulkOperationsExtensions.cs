using Npgsql;
using ThisShard.Database.Mappers;
using ThisShard.Database.Models.Columns;
using ThisShard.Database.Models.Rows;
using ThisShard.Database.Models.Tables;
using ThisShard.Database.Options;
using ThisShard.Database.Readers;
using ThisShard.Database.Writers;

namespace ThisShard.Database.Extensions;

/// <summary>
/// Расширения Npgsql для массовой записи строк
/// </summary>
public static class NpgsqlBulkOperationsExtensions
{
    #region BatchWrite

    /// <summary>
    /// Произвести запись строк с использованием батчинка
    /// </summary>
    public static async ValueTask BatchWrite(this NpgsqlConnection connection, string[] tablePath, Func<IWriter, ValueTask> writing, NpgsqlBulkOperationsOptions? options = null)
    {
        await connection.Write(
            cn => GetBatchWriter(cn, tablePath, options),
            writing
        );
    }
    
    /// <summary>
    /// Произвести запись строк с использованием батчинка
    /// </summary>
    public static async ValueTask BatchWrite(this NpgsqlConnection connection, PgTable table, Func<IWriter, ValueTask> writing, NpgsqlBulkOperationsOptions? options = null)
    {
        await connection.Write(
            cn => cn.GetBatchWriter(table, options),
            writing
        );
    }
    
    /// <summary>
    /// Произвести запись строк с использованием батчинка
    /// </summary>
    public static async ValueTask BatchWrite(this NpgsqlConnection connection, PgStagingTable table, Func<IWriter, ValueTask> writing, NpgsqlBulkOperationsOptions? options = null)
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
    public static async ValueTask BulkWrite(this NpgsqlConnection connection, string[] tablePath, Func<IWriter, ValueTask> writing, NpgsqlBulkOperationsOptions? options = null)
    {
        await connection.Write(
            cn => cn.GetBulkWriter(tablePath, options),
            writing
        );
    }

    /// <summary>
    /// Произвести запись строк с использованием Bulk операций
    /// </summary>
    public static async ValueTask BulkWrite(this NpgsqlConnection connection, PgTable table, Func<IWriter, ValueTask> writing, NpgsqlBulkOperationsOptions? options = null)
    {
        await connection.Write(
            cn => cn.GetBulkWriter(table, options),
            writing
        );
    }

    /// <summary>
    /// Произвести запись строк с использованием Bulk операций
    /// </summary>
    public static async ValueTask BulkWrite(this NpgsqlConnection connection, PgStagingTable table, Func<IWriter, ValueTask> writing, NpgsqlBulkOperationsOptions? options = null)
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
    public static async ValueTask<IWriter> GetBatchWriter(this NpgsqlConnection connection, string[] tablePath, NpgsqlBulkOperationsOptions? options = null)
    {
        options ??= NpgsqlBulkOperationsOptions.Default;

        var table = await options.TableManager.GetTable(connection, tablePath);
        if (table == null)
            throw new InvalidOperationException("Table not exists");

        var writer = CreateWriter(
            connection,
            cn => new PgBatchWriter(cn, table, options.BatchBufferSize, options.CommandFilterFactory),
            options,
            options.ValueConverter?.CreateRowMapper(table)
        );
        
        try
        {
            await writer.Init();
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
    public static async ValueTask<IWriter> GetBatchWriter(this NpgsqlConnection connection, PgTable table, NpgsqlBulkOperationsOptions? options = null)
    {
        options ??= NpgsqlBulkOperationsOptions.Default;

        var writer = CreateWriter(
            connection,
            cn => new PgBatchWriter(cn, table, options.BatchBufferSize, options.CommandFilterFactory),
            options,
            options.ValueConverter?.CreateRowMapper(table)
        );
        
        try
        {
            await writer.Init();
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
    public static async ValueTask<IWriter> GetBatchWriter(this NpgsqlConnection connection, PgStagingTable table, NpgsqlBulkOperationsOptions? options = null)
    {
        options ??= NpgsqlBulkOperationsOptions.Default;

        var writer = CreateWriter(
            connection,
            cn => new PgBatchWriter(cn, table.DestinationTable, options.BatchBufferSize, options.CommandFilterFactory),
            options,
            options.ValueConverter?.CreateRowMapper(table.DestinationTable)
        );
        
        try
        {
            await writer.Init();
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
    public static async ValueTask<IWriter> GetBulkWriter(this NpgsqlConnection connection, string[] tablePath, NpgsqlBulkOperationsOptions? options = null)
    {
        options ??= NpgsqlBulkOperationsOptions.Default;

        var table = await options.TableManager.GetTable(connection, tablePath);
        if (table == null)
            throw new InvalidOperationException("Table not exists");

        var writer = CreateWriter(
            connection,
            cn =>
            {
                var descriptors = GetBulkDescriptors(cn, table, options);
                var bufferSize = GetMaxBufferSize(options);
                return new CompositeWriter(descriptors, bufferSize);
            },
            options,
            options.ValueConverter?.CreateRowMapper(table)
        );
        
        try
        {
            await writer.Init();
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
    public static async ValueTask<IWriter> GetBulkWriter(this NpgsqlConnection connection, PgTable table, NpgsqlBulkOperationsOptions? options = null)
    {
        options ??= NpgsqlBulkOperationsOptions.Default;

        var writer = CreateWriter(
            connection,
            cn =>
            {
                var descriptors = GetBulkDescriptors(cn, table, options);
                var bufferSize = GetMaxBufferSize(options);
                return new CompositeWriter(descriptors, bufferSize);
            },
            options,
            options.ValueConverter?.CreateRowMapper(table)
        );
        
        try
        {
            await writer.Init();
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
    public static async ValueTask<IWriter> GetBulkWriter(this NpgsqlConnection connection, PgStagingTable table, NpgsqlBulkOperationsOptions? options = null)
    {
        options ??= NpgsqlBulkOperationsOptions.Default;

        var writer = CreateWriter(
            connection,
            cn =>
            {
                var descriptors = GetBulkDescriptors(cn, table, options);
                var bufferSize = GetMaxBufferSize(options);
                return new CompositeWriter(descriptors, bufferSize);
            },
            options,
            options.ValueConverter?.CreateRowMapper(table.DestinationTable)
        );
        
        try
        {
            await writer.Init();
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
    
    #region GetSustainableReader

    /// <summary>
    /// Возвращает читателя
    /// </summary>
    public static async ValueTask<IReader> GetSustainableReader(this NpgsqlConnection connection,
        string[] tablePath,
        RowState rowState = RowState.Added,
        NpgsqlBulkOperationsOptions? options = null,
        bool ownsConnection = false,
        IRow? startRow = null)
    {
        var table = await connection.GetTableInfo(tablePath, options);
        if (table == null)
            throw new InvalidOperationException("Table not exists");
        
        return connection.GetSustainableReader(table, rowState, options, ownsConnection, startRow);
    }
    
    /// <summary>
    /// Возвращает читателя
    /// </summary>
    public static async ValueTask<IReader> GetSustainableReader(this NpgsqlConnection connection,
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
        
        return connection.GetSustainableReader(table, commandFactory, rowState, options, ownsConnection, startRow);
    }
    
    /// <summary>
    /// Возвращает читателя
    /// </summary>
    public static IReader GetSustainableReader(this NpgsqlConnection connection,
        PgTable table,
        RowState rowState = RowState.Added,
        NpgsqlBulkOperationsOptions? options = null,
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
    public static IReader GetSustainableReader(this NpgsqlConnection connection,
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
        return connection.GetSustainableReader(async (cn, row, writer, ct) =>
        {
            await using var command = commandFactory(cn);
            AdjustCommand(command, row ?? startRow, primaryKey);
            await using var reader = await command.ExecuteReaderAsync(ct).GetReader(rowState);
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
    private static IWriter CreateWriter(NpgsqlConnection connection, Func<NpgsqlConnection, IWriter> factory, NpgsqlBulkOperationsOptions options, IRowMapper? mapper)
    {
        var writer = options.SustainableOptions == null 
            ? factory(connection) 
            : connection.GetSustainableWriter(factory, options.SustainableOptions);

        if (mapper != null)
            writer = writer.UsingMapper(mapper);
        
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
    private static CompositeWriterDescriptor[] GetBulkDescriptors(NpgsqlConnection connection, PgTable table,
        NpgsqlBulkOperationsOptions options)
    {
        return 
        [
            new()
            {
                MinRows = options.BulkMinRows,
                Owned = true,
                Writer = new PgBulkWriter(connection, table, options.StagingTableManager, options.BulkBufferSize, options.CommandFilterFactory)
            },
            new()
            {
                Owned = true,
                Writer = new PgBatchWriter(connection, table, options.BatchBufferSize, options.CommandFilterFactory)
            },
        ];
    }

    /// <summary>
    /// Возвращает дескрипторы для Bulk
    /// </summary>
    private static CompositeWriterDescriptor[] GetBulkDescriptors(NpgsqlConnection connection, PgStagingTable table,
        NpgsqlBulkOperationsOptions options)
    {
        return 
        [
            new()
            {
                MinRows = options.BulkMinRows,
                Owned = true,
                Writer = new PgBulkWriter(connection, table, options.StagingTableManager, options.BulkBufferSize, options.CommandFilterFactory)
            },
            new()
            {
                Owned = true,
                Writer = new PgBatchWriter(connection, table.DestinationTable, options.BatchBufferSize, options.CommandFilterFactory)
            },
        ];
    }
    
    #endregion
}