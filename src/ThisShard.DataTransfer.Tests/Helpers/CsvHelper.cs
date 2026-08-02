using System.Data.Common;
using System.Globalization;
using System.IO.Compression;
using CsvHelper;
using ThisShard.Database.Core.Extensions;
using ThisShard.Database.Infrastructure.Extensions;

namespace ThisShard.Database.Tests.Helpers;

public static class CsvHelper
{
    /// <summary>
    /// Возвращает количество строк в json
    /// </summary>
    public static async Task<long> GetRowsCount(CsvReader connection)
    {
        await using var reader = connection.GetRowReader(ownsReader: false);
        var rows = await reader.ReadToEnd();
        return rows.Count;
    }
    
    /// <summary>
    /// Проверяет забэкапились ли данные
    /// </summary>
    public static async Task AssertDataDumped(DbConnection srcCn, ZipArchive dumpCn)
    {
        foreach (var entry in dumpCn.Entries)
        {
            await using var entryStream = entry.Open();
            using var reader = new CsvReader(new StreamReader(entryStream), CultureInfo.InvariantCulture);
            await AssertDataDumped(srcCn, reader, entry.Name);
        }
    }
    
    /// <summary>
    /// Проверяет забэкапились ли данные
    /// </summary>
    public static async Task AssertDataDumped(DbConnection srcCn, CsvReader dumpCn, string tableName)
    {
        var expected = await DbHelper.GetRowsCount(srcCn, tableName);
        var actual = await GetRowsCount(dumpCn);
        Assert.Equal(expected, actual);
    }
    
    /// <summary>
    /// Проверяет восстановились ли данные
    /// </summary>
    public static async Task AssertDataRestored(DbConnection srcCn, ZipArchive dumpCn)
    {
        foreach (var entry in dumpCn.Entries)
        {
            await using var entryStream = entry.Open();
            using var reader = new CsvReader(new StreamReader(entryStream), CultureInfo.InvariantCulture);
            await AssertDataRestored(srcCn, reader, entry.Name);
        }
    }
    
    /// <summary>
    /// Проверяет восстановились ли данные
    /// </summary>
    public static async Task AssertDataRestored(DbConnection srcCn, CsvReader dumpCn, string tableName)
    {
        var actual = await DbHelper.GetRowsCount(srcCn, tableName);
        var expected = await GetRowsCount(dumpCn);
        Assert.Equal(expected, actual);
    }
    
    /// <summary>
    /// Пишет данные всех таблиц в Csv
    /// </summary>
    public static async Task<Dictionary<string, TTable>> DumpDataToCsv<TConnection, TTable>(TConnection srcCn, ZipArchive dumpCn, IEnumerable<string> tableNames, Func<TConnection, CsvWriter, string, Task<TTable>> dumpFunc)
    {
        var tables = new Dictionary<string, TTable>();
        
        foreach (var tableName in tableNames)
        {
            var entry = dumpCn.CreateEntry(tableName);
            await using var entryStream = entry.Open();
            await using var writer = new CsvWriter(new StreamWriter(entryStream), CultureInfo.InvariantCulture);
            var table = await dumpFunc(srcCn, writer, tableName);
            tables.Add(tableName, table);
        }
        
        return tables;
    }
    
    /// <summary>
    /// Восстанавливает данные из Csv
    /// </summary>
    public static async Task RestoreDataFromCsv<TConnection, TTable>(TConnection srcCn, ZipArchive dumpCn, Dictionary<string, TTable> tables, Func<TConnection, CsvReader, TTable, Task> restoreFunc)
    {
        foreach (var entry in dumpCn.Entries)
        {
            await using var entryStream = entry.Open();
            using var reader = new CsvReader(new StreamReader(entryStream), CultureInfo.InvariantCulture);
            var table = tables[entry.Name];
            await restoreFunc(srcCn, reader, table);
        }
    }
}