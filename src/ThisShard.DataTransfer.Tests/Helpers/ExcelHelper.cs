using System.Data.Common;
using ExcelDataReader;
using LargeXlsx;
using ThisShard.Database.Core.Extensions;
using ThisShard.Database.Infrastructure.Extensions;

namespace ThisShard.Database.Tests.Helpers;

public static class ExcelHelper
{
    /// <summary>
    /// Возвращает количество строк в json
    /// </summary>
    public static async Task<long> GetRowsCount(IExcelDataReader connection)
    {
        await using var reader = connection.GetRowReader(ownsReader: false);
        var rows = await reader.ReadToEnd();
        return rows.Count;
    }
    
    /// <summary>
    /// Проверяет забэкапились ли данные
    /// </summary>
    public static async Task AssertDataDumped(DbConnection srcCn, IExcelDataReader dumpCn)
    {
        do
        {
            await AssertDataDumped(srcCn, dumpCn, dumpCn.Name);
        } while (dumpCn.NextResult());
    }
    
    /// <summary>
    /// Проверяет забэкапились ли данные
    /// </summary>
    public static async Task AssertDataDumped(DbConnection srcCn, IExcelDataReader dumpCn, string tableName)
    {
        var expected = await DbHelper.GetRowsCount(srcCn, tableName);
        var actual = await GetRowsCount(dumpCn);
        Assert.Equal(expected, actual);
    }
    
    /// <summary>
    /// Проверяет восстановились ли данные
    /// </summary>
    public static async Task AssertDataRestored(DbConnection srcCn, IExcelDataReader dumpCn)
    {
        do
        {
            await AssertDataRestored(srcCn, dumpCn, dumpCn.Name);
        } while (dumpCn.NextResult());
    }
    
    /// <summary>
    /// Проверяет восстановились ли данные
    /// </summary>
    public static async Task AssertDataRestored(DbConnection srcCn, IExcelDataReader dumpCn, string tableName)
    {
        var actual = await DbHelper.GetRowsCount(srcCn, tableName);
        var expected = await GetRowsCount(dumpCn);
        Assert.Equal(expected, actual);
    }
    
    /// <summary>
    /// Пишет данные всех таблиц в Excel
    /// </summary>
    public static async Task<Dictionary<string, TTable>> DumpDataToExcel<TConnection, TTable>(TConnection srcCn, XlsxWriter dumpCn, IEnumerable<string> tableNames, Func<TConnection, XlsxWriter, string, Task<TTable>> dumpFunc)
    {
        var tables = new Dictionary<string, TTable>();
        
        foreach (var tableName in tableNames)
        {
            await dumpCn.BeginWorksheetAsync(tableName);
            var table = await dumpFunc(srcCn, dumpCn, tableName);
            tables.Add(tableName, table);
        }
        
        return tables;
    }
    
    /// <summary>
    /// Восстанавливает данные из Excel
    /// </summary>
    public static async Task RestoreDataFromExcel<TConnection, TTable>(TConnection srcCn, IExcelDataReader dumpCn, Dictionary<string, TTable> tables, Func<TConnection, IExcelDataReader, TTable, Task> restoreFunc)
    {
        do
        {
            var table = tables[dumpCn.Name];
            await restoreFunc(srcCn, dumpCn, table);
        } while (dumpCn.NextResult());
    }
}