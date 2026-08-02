using Microsoft.Data.Sqlite;
using Npgsql;
using ThisShard.Database.Core.Extensions;
using ThisShard.Database.Core.Models.Rows;
using ThisShard.Database.Core.Readers;
using ThisShard.Database.Infrastructure.Extensions;

namespace ThisShard.Database.Tests.Helpers;

public static class SqliteHelper
{
    public static ValueTask<IRowReader> GetReader(SqliteConnection connection, string tableName, RowState rowState = RowState.AddedOrModified)
    {
        return connection.GetSustainableRowReader(tableName, rowState);
    }
    

    public static async ValueTask<List<IRow>> GetRows(SqliteConnection connection, string path,
        RowState rowState = RowState.AddedOrModified)
    {
        await using var reader = await connection.GetSustainableRowReader(path, rowState);
        return await reader.ReadToEnd();
    }

    public static async Task UsingTestTable(SqliteConnection cn, string name, Func<Task> action)
    {
        try
        {
            await CreateTestTable(cn, name);
            await action();
        }
        finally
        {
            try
            {
                await DropTestTable(cn, name);
            }
            catch (PostgresException)
            {
                //Игнорируем ошибку
            }
        }
    }
    
    public static IEnumerable<Row> GenerateTestRows(RowState rowState, int count)
    {
        return Enumerable.Range(1, count).Select(x => new Row
        {
            State = rowState,
            Data = new Dictionary<string, object?>
            {
                ["id"] = x,
                ["date"] = $"test_{rowState}_{x}"
            }
        });
    }
    
    public static async Task CreateTestTable(SqliteConnection cn, string name)
    {
        await DropTestTable(cn, name);
        
        await using var cmd = cn.CreateCommand();
        cmd.CommandText = $"""
                           CREATE TABLE {name} 
                           (
                               id integer primary key, 
                               date text
                           );
                           """;
        
        await cmd.ExecuteNonQueryAsync();
    }

    public static async Task DropTestTable(SqliteConnection cn, string name)
    {
        await using var cmd = cn.CreateCommand();
        cmd.CommandText = $"""
                           DROP TABLE IF EXISTS {name};
                           """;
        
        await cmd.ExecuteNonQueryAsync();
    }
}