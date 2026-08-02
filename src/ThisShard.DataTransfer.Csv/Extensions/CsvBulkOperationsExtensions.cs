using CsvHelper;
using ThisShard.Database.Core.Extensions;
using ThisShard.Database.Core.Models.Rows;
using ThisShard.Database.Core.Models.Tables;
using ThisShard.Database.Core.Writers;
using ThisShard.Database.Infrastructure.Csv.Models;
using ThisShard.Database.Infrastructure.Csv.Options;
using ThisShard.Database.Infrastructure.Csv.Readers;
using ThisShard.Database.Infrastructure.Csv.Tables;
using ThisShard.Database.Infrastructure.Csv.Writers;

namespace ThisShard.Database.Infrastructure.Extensions;

/// <summary>
/// Расширения для Csv
/// </summary>
public static class CsvBulkOperationsExtensions
{
    #region Write

    /// <summary>
    /// Произвести запись строк
    /// </summary>
    public static ValueTask Write(this CsvWriter writer, Type type, Func<IRowWriter, ValueTask> writing, CsvBulkOperationsOptions? options = null) =>
        writer.Write(provider => provider.GetTable(type), writing, options);
    
    /// <summary>
    /// Произвести запись строк
    /// </summary>
    public static async ValueTask Write(this CsvWriter writer, Func<ICsvTableProvider, CsvTable> tableFactory, Func<IRowWriter, ValueTask> writing, CsvBulkOperationsOptions? options = null) 
    {
        options ??= CsvBulkOperationsOptions.Default;

        var CsvTable = tableFactory(options.TableProvider);
        if (CsvTable == null)
            throw new InvalidOperationException("Can't convert table to Csv table");

        await writer.Write(CsvTable, writing, options);
    }
    
    /// <summary>
    /// Произвести запись строк
    /// </summary>
    public static async ValueTask Write(this CsvWriter writer, ITable table, Func<IRowWriter, ValueTask> writing, CsvBulkOperationsOptions? options = null) 
    {
        options ??= CsvBulkOperationsOptions.Default;

        var CsvTable = options.TableProvider.ConvertTable(table);
        if (CsvTable == null)
            throw new InvalidOperationException("Can't convert table to Csv table");

        await writer.Write(CsvTable, writing, options);
    }
    
    /// <summary>
    /// Произвести запись строк
    /// </summary>
    public static async ValueTask Write(this CsvWriter writer, CsvTable table, Func<IRowWriter, ValueTask> writing, CsvBulkOperationsOptions? options = null)
    {
        options ??= CsvBulkOperationsOptions.Default;
        await using var tableWriter = await writer.GetTableWriter(table, options);
        await writing(tableWriter);
        await tableWriter.Complete();
    }
    
    #endregion
    
    #region GetWriter
    
    /// <summary>
    /// Возвращает писателя для типа
    /// </summary>
    public static async ValueTask<ITableWriter> GetTableWriter(this CsvWriter writer, Type entityType, CsvBulkOperationsOptions? options = null)
    {
        options ??= CsvBulkOperationsOptions.Default;

        var CsvTable = options.TableProvider.GetTable(entityType);
        if (CsvTable == null)
            throw new InvalidOperationException("Can't convert type to Csv table");

        return await writer.GetTableWriter(CsvTable, options);
    }
    
    /// <summary>
    /// Возвращает писателя таблицы
    /// </summary>
    public static async ValueTask<ITableWriter> GetTableWriter(this CsvWriter writer, ITable table, CsvBulkOperationsOptions? options = null)
    {
        options ??= CsvBulkOperationsOptions.Default;

        var CsvTable = options.TableProvider.ConvertTable(table);
        if (CsvTable == null)
            throw new InvalidOperationException("Can't convert table to Csv table");

        return await writer.GetTableWriter(CsvTable, options);
    }

    /// <summary>
    /// Возвращает писателя таблицы
    /// </summary>
    public static async ValueTask<ITableWriter> GetTableWriter(this CsvWriter writer, CsvTable table, CsvBulkOperationsOptions? options = null)
    {
        options ??= CsvBulkOperationsOptions.Default;

        ITableWriter CsvWriter = new CsvTableWriter(writer, options.RowFilter, options.RowStatePropertyName);
        try
        {
            await CsvWriter.Init(table);
        }
        catch (Exception)
        {
            await CsvWriter.DisposeAsync();
            throw;
        }

        if (options.ValueConverter != null)
            CsvWriter = CsvWriter.UsingValueConverter(options.ValueConverter);
        
        return CsvWriter;
    }
    
    #endregion
    
    #region GetReader

    /// <summary>
    /// Возвращает писателя таблицы
    /// </summary>
    public static CsvRowReader GetRowReader(this CsvReader reader,
        RowState defaultRowState = RowState.Added, Func<string, string>? propertyNameResolver = null,
        string? rowStatePropertyName = null, bool ownsReader = true)
    {
        return new CsvRowReader(reader, defaultRowState, propertyNameResolver, rowStatePropertyName, ownsReader);
    }

    #endregion
}