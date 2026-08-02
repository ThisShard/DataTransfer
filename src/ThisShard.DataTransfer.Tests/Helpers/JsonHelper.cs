using System.Data.Common;
using System.Text.Json;
using System.Text.Json.Stream;
using ThisShard.Database.Core.Extensions;
using ThisShard.Database.Infrastructure.Extensions;

namespace ThisShard.Database.Tests.Helpers;

public static class JsonHelper
{
    /// <summary>
    /// Возвращает количество строк в json
    /// </summary>
    public static async Task<long> GetRowsCount(IUtf8JsonAsyncStreamReader connection)
    {
        await using var reader = connection.GetRowReader(ownsReader: false);
        var rows = await reader.ReadToEnd();
        return rows.Count;
    }
    
    /// <summary>
    /// Проверяет забэкапились ли данные
    /// </summary>
    public static async Task AssertDataDumped(DbConnection srcCn, Utf8JsonAsyncStreamReader dumpCn)
    {
        await dumpCn.ReadAsync();
        Assert.Equal(JsonTokenType.StartObject, dumpCn.TokenType);
        while (await dumpCn.ReadAsync())
        {
            if (dumpCn.TokenType == JsonTokenType.EndObject)
                break;
                
            Assert.Equal(JsonTokenType.PropertyName, dumpCn.TokenType);
            var tableName = dumpCn.GetString()!;
            await AssertDataDumped(srcCn, dumpCn, tableName);
        }
    }
    
    /// <summary>
    /// Проверяет забэкапились ли данные
    /// </summary>
    public static async Task AssertDataDumped(DbConnection srcCn, IUtf8JsonAsyncStreamReader dumpCn, string tableName)
    {
        var expected = await DbHelper.GetRowsCount(srcCn, tableName);
        var actual = await GetRowsCount(dumpCn);
        Assert.Equal(expected, actual);
    }
    
    /// <summary>
    /// Проверяет восстановились ли данные
    /// </summary>
    public static async Task AssertDataRestored(DbConnection srcCn, Utf8JsonAsyncStreamReader dumpCn)
    {
        await dumpCn.ReadAsync();
        Assert.Equal(JsonTokenType.StartObject, dumpCn.TokenType);
        while (await dumpCn.ReadAsync())
        {
            if (dumpCn.TokenType == JsonTokenType.EndObject)
                break;
                
            Assert.Equal(JsonTokenType.PropertyName, dumpCn.TokenType);
            var tableName = dumpCn.GetString()!;
            await AssertDataRestored(srcCn, dumpCn, tableName);
        }
    }
    
    /// <summary>
    /// Проверяет восстановились ли данные
    /// </summary>
    public static async Task AssertDataRestored(DbConnection srcCn, IUtf8JsonAsyncStreamReader dumpCn, string tableName)
    {
        var actual = await DbHelper.GetRowsCount(srcCn, tableName);
        var expected = await GetRowsCount(dumpCn);
        Assert.Equal(expected, actual);
    }
    
    /// <summary>
    /// Пишет данные всех таблиц в Json
    /// </summary>
    public static async Task<Dictionary<string, TTable>> DumpDataToJson<TConnection, TTable>(TConnection srcCn, Utf8JsonWriter dumpCn, IEnumerable<string> tableNames, Func<TConnection, Utf8JsonWriter, string, Task<TTable>> dumpFunc)
    {
        var tables = new Dictionary<string, TTable>();
        
        dumpCn.WriteStartObject();
        
        foreach (var tableName in tableNames)
        {
            dumpCn.WritePropertyName(tableName);
            var table = await dumpFunc(srcCn, dumpCn, tableName);
            tables[tableName] = table;
        }
        
        dumpCn.WriteEndObject();
        
        await dumpCn.FlushAsync();
        
        return tables;
    }
    
    /// <summary>
    /// Восстанавливает данные из Json
    /// </summary>
    public static async Task RestoreDataFromJson<TConnection, TTable>(TConnection srcCn, Utf8JsonAsyncStreamReader dumpCn, Dictionary<string, TTable> tables, Func<TConnection, Utf8JsonAsyncStreamReader, TTable, Task> restoreFunc)
    {
        await dumpCn.ReadAsync();
        Assert.Equal(JsonTokenType.StartObject, dumpCn.TokenType);
        while (await dumpCn.ReadAsync())
        {
            if (dumpCn.TokenType == JsonTokenType.EndObject)
                break;
                
            Assert.Equal(JsonTokenType.PropertyName, dumpCn.TokenType);
            var tableName = dumpCn.GetString()!;
            var table = tables[tableName];
            await restoreFunc(srcCn, dumpCn, table);
        }
    }
}