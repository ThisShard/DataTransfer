using System.Data;
using System.Data.Common;

namespace ThisShard.Database.Tests.Helpers;

public static class DbHelper
{
    /// <summary>
    /// Возвращает количество строк
    /// </summary>
    public static async Task<long> GetRowsCount(DbConnection connection, string tableName)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = $"SELECT count(*) FROM \"{tableName}\"";
        await using var reader = await command.ExecuteReaderAsync(CommandBehavior.SingleRow);
        if (!await reader.ReadAsync())
            return 0;
        
        return reader.GetInt64(0);
    }
    
    /// <summary>
    /// Проверяет забэкапились ли данные
    /// </summary>
    public static async Task AssertDataDumped(DbConnection srcCn, DbConnection dumpCn, IEnumerable<string> tableNames)
    {
        foreach (var tableName in tableNames)
        {
            await AssertDataDumped(srcCn, dumpCn, tableName);
        }
    }
    
    /// <summary>
    /// Проверяет забэкапились ли данные
    /// </summary>
    public static async Task AssertDataDumped(DbConnection srcCn, DbConnection dumpCn, string tableName)
    {
        var expected = await GetRowsCount(srcCn, tableName);
        var actual = await GetRowsCount(dumpCn, tableName);
        Assert.Equal(expected, actual);
    }
    
    /// <summary>
    /// Проверяет восстановились ли данные
    /// </summary>
    public static async Task AssertDataRestored(DbConnection srcCn, DbConnection dumpCn, IEnumerable<string> tableNames)
    {
        foreach (var tableName in tableNames)
        {
            await AssertDataRestored(srcCn, dumpCn, tableName);
        }
    }
    
    /// <summary>
    /// Проверяет восстановились ли данные
    /// </summary>
    public static async Task AssertDataRestored(DbConnection srcCn, DbConnection dumpCn, string tableName)
    {
        var actual = await GetRowsCount(srcCn, tableName);
        var expected = await GetRowsCount(dumpCn, tableName);
        Assert.Equal(expected, actual);
    }
}