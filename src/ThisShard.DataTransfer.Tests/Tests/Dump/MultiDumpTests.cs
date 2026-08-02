using System.Text.Json;
using System.Text.Json.Stream;
using Microsoft.Data.Sqlite;
using Npgsql;
using ThisShard.Database.Core.Extensions;
using ThisShard.Database.Core.Models.Rows;
using ThisShard.Database.Infrastructure.Extensions;
using ThisShard.Database.Infrastructure.Json.Options;
using ThisShard.Database.Infrastructure.Sqlite.Options;
using ThisShard.Database.Tests.Helpers;

namespace ThisShard.Database.Tests.Tests.Dump;

public class MultiDumpTests
{
    private const string PgConnectionString =
        "Host=localhost;port=5432;Database=kdb;Username=postgres;Password=postgres;Include Error Detail=true;";
    private const string SqliteConnectionString =
        //"Data Source=/Users/shard/Documents/dump.db";
        "Data Source=:memory:";
    
    [Fact]
    public async Task MultiDumpTest()
    {
        //Arrange
        var tableName = "Messages";
        
        await using var postgresCn = GetPostgresConnection();
        await postgresCn.OpenAsync();
        
        await using var sqliteCn = GetSqliteConnection();
        await sqliteCn.OpenAsync();
        
        await using var ms = new MemoryStream();
        
        
        //Act
        var row = await DumpData(postgresCn, tableName, sqliteCn, ms);

        //Assert
        Assert.Null(row);
        await DbHelper.AssertDataDumped(postgresCn, sqliteCn, tableName);
        ms.Seek(0, SeekOrigin.Begin);
        using var jsonReader = new Utf8JsonAsyncStreamReader(ms);
        await JsonHelper.AssertDataDumped(postgresCn, jsonReader, tableName);
    }

    private async Task<IRow?> DumpData(NpgsqlConnection postgresCn, string tableName, SqliteConnection sqliteCn,
        MemoryStream ms)
    {
        var table = await postgresCn.GetTableInfo([tableName]);
        await using var reader = PostgresHelper.GetReader(postgresCn, table!);
        await using var sqliteWriter = await sqliteCn.CreateTableAndGetWriter(table!);
        await using var dumpCn = new Utf8JsonWriter(ms, new JsonWriterOptions { Indented = true });
        await using var jsonWriter = await dumpCn.GetTableWriter(table!);
        var row = await reader.WriteTo([
            sqliteWriter.UsingValueConverter(SqliteBulkOperationsOptions.Default.ValueConverter!), 
            jsonWriter.UsingValueConverter(JsonBulkOperationsOptions.Default.ValueConverter!)
        ]);
        await sqliteWriter.Complete();
        await jsonWriter.Complete();
        return row;
    }

    private NpgsqlConnection GetPostgresConnection() => new(PgConnectionString);
    
    private SqliteConnection GetSqliteConnection() => new(SqliteConnectionString);
}