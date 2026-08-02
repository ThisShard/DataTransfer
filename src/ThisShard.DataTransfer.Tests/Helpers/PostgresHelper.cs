using Npgsql;
using ThisShard.Database.Core.Extensions;
using ThisShard.Database.Core.Models.Rows;
using ThisShard.Database.Core.Readers;
using ThisShard.Database.Infrastructure.Extensions;
using ThisShard.Database.Infrastructure.Postgres.Models;

namespace ThisShard.Database.Tests.Helpers;

public static class PostgresHelper
{
    public static IRowReader GetReader(NpgsqlConnection connection, PgTable table, RowState rowState = RowState.AddedOrModified)
    {
        return connection.GetSustainableRowReader(table, rowState);
    }

    public static async ValueTask<List<IRow>> GetRows(NpgsqlConnection connection, string[] path,
        RowState rowState = RowState.AddedOrModified)
    {
        await using var reader = await connection.GetSustainableRowReader(path, rowState);
        return await reader.ReadToEnd();
    }
    
    /// <summary>
    /// Чистит таблицы
    /// </summary>
    public static async Task CleanupTables(NpgsqlConnection postgresCn, params string[] tableNames)
    {
        await using var command = postgresCn.CreateCommand();
        command.CommandText = $"TRUNCATE TABLE {string.Join(", ", tableNames.Select(x=>$"\"{x}\""))}";
        await command.ExecuteNonQueryAsync();
    }

    public static async Task UsingTestTable(NpgsqlConnection cn, string name, Func<Task> action)
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
    
    public static async Task CreateTestTable(NpgsqlConnection cn, string name)
    {
        await DropTestTable(cn, name);
        
        await using var cmd = cn.CreateCommand();
        cmd.CommandText = $"""
                           CREATE TABLE public.{name} 
                           (
                               id int4 primary key, 
                               date text
                           );
                           """;
        
        await cmd.ExecuteNonQueryAsync();
    }

    public static async Task DropTestTable(NpgsqlConnection cn, string name)
    {
        await using var cmd = cn.CreateCommand();
        cmd.CommandText = $"""
                           DROP TABLE IF EXISTS public.{name} CASCADE;
                           """;
        
        await cmd.ExecuteNonQueryAsync();
    }
}