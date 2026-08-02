using ExcelDataReader;
using LargeXlsx;
using ThisShard.Database.Core.Extensions;
using ThisShard.Database.Core.Models.Rows;
using ThisShard.Database.Core.Models.Tables;
using ThisShard.Database.Core.Writers;
using ThisShard.Database.Infrastructure.Excel.Models;
using ThisShard.Database.Infrastructure.Excel.Options;
using ThisShard.Database.Infrastructure.Excel.Readers;
using ThisShard.Database.Infrastructure.Excel.Tables;
using ThisShard.Database.Infrastructure.Excel.Writers;

namespace ThisShard.Database.Infrastructure.Extensions;

/// <summary>
/// Расширения для Excel
/// </summary>
public static class ExcelBulkOperationsExtensions
{
    #region Write

    /// <summary>
    /// Произвести запись строк
    /// </summary>
    public static ValueTask Write(this XlsxWriter writer, Type type, Func<IRowWriter, ValueTask> writing, ExcelBulkOperationsOptions? options = null) =>
        writer.Write(provider => provider.GetTable(type), writing, options);
    
    /// <summary>
    /// Произвести запись строк
    /// </summary>
    public static async ValueTask Write(this XlsxWriter writer, Func<IExcelTableProvider, ExcelTable> tableFactory, Func<IRowWriter, ValueTask> writing, ExcelBulkOperationsOptions? options = null) 
    {
        options ??= ExcelBulkOperationsOptions.Default;

        var excelTable = tableFactory(options.TableProvider);
        if (excelTable == null)
            throw new InvalidOperationException("Can't convert table to Excel table");

        await writer.Write(excelTable, writing, options);
    }
    
    /// <summary>
    /// Произвести запись строк
    /// </summary>
    public static async ValueTask Write(this XlsxWriter writer, ITable table, Func<IRowWriter, ValueTask> writing, ExcelBulkOperationsOptions? options = null) 
    {
        options ??= ExcelBulkOperationsOptions.Default;

        var excelTable = options.TableProvider.ConvertTable(table);
        if (excelTable == null)
            throw new InvalidOperationException("Can't convert table to Excel table");

        await writer.Write(excelTable, writing, options);
    }
    
    /// <summary>
    /// Произвести запись строк
    /// </summary>
    public static async ValueTask Write(this XlsxWriter writer, ExcelTable table, Func<IRowWriter, ValueTask> writing, ExcelBulkOperationsOptions? options = null)
    {
        options ??= ExcelBulkOperationsOptions.Default;
        await using var tableWriter = await writer.GetTableWriter(table, options);
        await writing(tableWriter);
        await tableWriter.Complete();
    }
    
    #endregion
    
    #region GetWriter
    
    /// <summary>
    /// Возвращает писателя для типа
    /// </summary>
    public static async ValueTask<ITableWriter> GetTableWriter(this XlsxWriter writer, Type entityType, ExcelBulkOperationsOptions? options = null)
    {
        options ??= ExcelBulkOperationsOptions.Default;

        var excelTable = options.TableProvider.GetTable(entityType);
        if (excelTable == null)
            throw new InvalidOperationException("Can't convert type to Excel table");

        return await writer.GetTableWriter(excelTable, options);
    }
    
    /// <summary>
    /// Возвращает писателя таблицы
    /// </summary>
    public static async ValueTask<ITableWriter> GetTableWriter(this XlsxWriter writer, ITable table, ExcelBulkOperationsOptions? options = null)
    {
        options ??= ExcelBulkOperationsOptions.Default;

        var excelTable = options.TableProvider.ConvertTable(table);
        if (excelTable == null)
            throw new InvalidOperationException("Can't convert table to Excel table");

        return await writer.GetTableWriter(excelTable, options);
    }

    /// <summary>
    /// Возвращает писателя таблицы
    /// </summary>
    public static async ValueTask<ITableWriter> GetTableWriter(this XlsxWriter writer, ExcelTable table, ExcelBulkOperationsOptions? options = null)
    {
        options ??= ExcelBulkOperationsOptions.Default;

        ITableWriter xlsxWriter = new ExcelTableWriter(writer, options.Styles, options.RowFilter, options.RowStatePropertyName);
        try
        {
            await xlsxWriter.Init(table);
        }
        catch (Exception)
        {
            await xlsxWriter.DisposeAsync();
            throw;
        }

        if (options.ValueConverter != null)
            xlsxWriter = xlsxWriter.UsingValueConverter(options.ValueConverter);
        
        return xlsxWriter;
    }
    
    #endregion
    
    #region GetReader

    /// <summary>
    /// Возвращает писателя таблицы
    /// </summary>
    public static ExcelRowReader GetRowReader(this IExcelDataReader reader,
        RowState defaultRowState = RowState.Added, Func<string, string>? propertyNameResolver = null,
        string? rowStatePropertyName = null, bool ownsReader = true)
    {
        return new ExcelRowReader(reader, defaultRowState, propertyNameResolver, rowStatePropertyName, ownsReader);
    }

    #endregion
}