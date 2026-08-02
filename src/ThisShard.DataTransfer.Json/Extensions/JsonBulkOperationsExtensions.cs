using System.Text.Json;
using System.Text.Json.Stream;
using ThisShard.Database.Core.Extensions;
using ThisShard.Database.Core.Models;
using ThisShard.Database.Core.Models.Rows;
using ThisShard.Database.Core.Models.Tables;
using ThisShard.Database.Core.Writers;
using ThisShard.Database.Infrastructure.Json.Models;
using ThisShard.Database.Infrastructure.Json.Options;
using ThisShard.Database.Infrastructure.Json.Readers;
using ThisShard.Database.Infrastructure.Json.Tables;
using ThisShard.Database.Infrastructure.Json.Writers;

namespace ThisShard.Database.Infrastructure.Extensions;

/// <summary>
/// Расширения Bulk операций для Json
/// </summary>
public static class JsonBulkOperationsExtensions
{
    #region Write

    /// <summary>
    /// Произвести запись строк
    /// </summary>
    public static ValueTask Write(this Utf8JsonWriter writer, Type type, Func<IRowWriter, ValueTask> writing, JsonBulkOperationsOptions? options = null) =>
        writer.Write(provider => provider.GetTable(type), writing, options);
    
    /// <summary>
    /// Произвести запись строк
    /// </summary>
    public static async ValueTask Write(this Utf8JsonWriter writer, Func<IJsonTableProvider, JsonTable> tableFactory, Func<IRowWriter, ValueTask> writing, JsonBulkOperationsOptions? options = null) 
    {
        options ??= JsonBulkOperationsOptions.Default;

        var jsonTable = tableFactory(options.TableProvider);
        if (jsonTable == null)
            throw new InvalidOperationException("Can't convert table to json table");

        await writer.Write(jsonTable, writing, options);
    }
    
    /// <summary>
    /// Произвести запись строк
    /// </summary>
    public static async ValueTask Write(this Utf8JsonWriter writer, ITable table, Func<IRowWriter, ValueTask> writing, JsonBulkOperationsOptions? options = null) 
    {
        options ??= JsonBulkOperationsOptions.Default;

        var jsonTable = options.TableProvider.ConvertTable(table);
        if (jsonTable == null)
            throw new InvalidOperationException("Can't convert table to json table");

        await writer.Write(jsonTable, writing, options);
    }
    
    /// <summary>
    /// Произвести запись строк
    /// </summary>
    public static async ValueTask Write(this Utf8JsonWriter writer, JsonTable table, Func<IRowWriter, ValueTask> writing, JsonBulkOperationsOptions? options = null)
    {
        options ??= JsonBulkOperationsOptions.Default;
        await using var tableWriter = await writer.GetTableWriter(table, options);
        await writing(tableWriter);
        await tableWriter.Complete();
    }
    
    #endregion
    
    #region GetWriter
    
    /// <summary>
    /// Возвращает писателя для типа
    /// </summary>
    public static async ValueTask<ITableWriter> GetTableWriter(this Utf8JsonWriter writer, Type entityType, JsonBulkOperationsOptions? options = null)
    {
        options ??= JsonBulkOperationsOptions.Default;

        var jsonTable = options.TableProvider.GetTable(entityType);
        if (jsonTable == null)
            throw new InvalidOperationException("Can't convert type to json table");

        return await writer.GetTableWriter(jsonTable, options);
    }
    
    /// <summary>
    /// Возвращает писателя таблицы
    /// </summary>
    public static async ValueTask<ITableWriter> GetTableWriter(this Utf8JsonWriter writer, ITable table, JsonBulkOperationsOptions? options = null)
    {
        options ??= JsonBulkOperationsOptions.Default;

        var jsonTable = options.TableProvider.ConvertTable(table);
        if (jsonTable == null)
            throw new InvalidOperationException("Can't convert table to json table");

        return await writer.GetTableWriter(jsonTable, options);
    }

    /// <summary>
    /// Возвращает писателя таблицы
    /// </summary>
    public static async ValueTask<ITableWriter> GetTableWriter(this Utf8JsonWriter writer, JsonTable table, JsonBulkOperationsOptions? options = null)
    {
        options ??= JsonBulkOperationsOptions.Default;

        ITableWriter jsonWriter = new JsonTableWriter(writer, options.RowFilter, options.RowStatePropertyName);
        try
        {
            await jsonWriter.Init(table);
        }
        catch (Exception)
        {
            await jsonWriter.DisposeAsync();
            throw;
        }

        if (options.ValueConverter != null)
            jsonWriter = jsonWriter.UsingValueConverter(options.ValueConverter);
        
        return jsonWriter;
    }
    
    #endregion
    
    #region GetReader

    /// <summary>
    /// Возвращает писателя таблицы
    /// </summary>
    public static JsonRowReader GetRowReader(this IUtf8JsonAsyncStreamReader jsonReader,
        RowState defaultRowState = RowState.Added, Func<string, string>? propertyNameResolver = null,
        string? rowStatePropertyName = null, bool ownsReader = true)
    {
        return new JsonRowReader(jsonReader, defaultRowState, propertyNameResolver, rowStatePropertyName, ownsReader);
    }

    #endregion
}