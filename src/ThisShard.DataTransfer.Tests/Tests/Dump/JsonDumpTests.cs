using System.Text.Json;
using System.Text.Json.Stream;
using Npgsql;
using ThisShard.Database.Core.Extensions;
using ThisShard.Database.Core.Models.Rows;
using ThisShard.Database.Infrastructure.Extensions;
using ThisShard.Database.Infrastructure.Postgres.Models;
using ThisShard.Database.Tests.Helpers;

namespace ThisShard.Database.Tests.Tests.Dump;

public class JsonDumpTests
{
    private const string PgConnectionString =
        "Host=localhost;port=5432;Database=kdb;Username=postgres;Password=postgres;Include Error Detail=true;";
    private const string RestoreConnectionString =
        "Host=localhost;port=5432;Database=kdbtest;Username=postgres;Password=postgres;Include Error Detail=true;";
    private const string JsonOutputFileName = "/Users/shard/Documents/dump.json";

    private static readonly string[] TablesToDump =
    [
        "Configurations",
        "Accounts",
        "DictAbonents",
        "Users",
        "IdentityTokens",
        "JournalTextTemplates",
        "JournalRecords",
        "Locks",
        "Messages",
        "NotificationTextTemplates",
        "Notification",
        "NotificationToUser",
    ];
    
    [Fact]
    public async Task DumpTest()
    {
        //Arrange
        await using var srcCn = GetPostgresConnection(PgConnectionString);
        await srcCn.OpenAsync();
        await using var ms = new MemoryStream();
        
        //Act
        await using var dumpCn = new Utf8JsonWriter(ms, new JsonWriterOptions { Indented = true });
        await JsonHelper.DumpDataToJson(srcCn, dumpCn, TablesToDump, DumpDataToJson);
        
        //Assert
        ms.Seek(0, SeekOrigin.Begin);
        using var assertCn = new Utf8JsonAsyncStreamReader(ms);
        await JsonHelper.AssertDataDumped(srcCn, assertCn);
    }

    [Fact]
    public async Task RestoreTest()
    {
        //Arrange
        await using var srcCn = GetPostgresConnection(PgConnectionString);
        await srcCn.OpenAsync();
        await using var restoreCn = GetPostgresConnection(RestoreConnectionString);
        await restoreCn.OpenAsync();
        await using var tran = await restoreCn.BeginTransactionAsync();
        await using var ms = new MemoryStream();
        
        //Act
        Dictionary<string, PgTable> tables;
        await using (var dumpCn = new Utf8JsonWriter(ms, new JsonWriterOptions { Indented = true }))
        {
            tables = await JsonHelper.DumpDataToJson(srcCn, dumpCn, TablesToDump, DumpDataToJson);
        }
        ms.Seek(0, SeekOrigin.Begin);
        using (var dumpCn = new Utf8JsonAsyncStreamReader(ms, leaveOpen: true))
        {
            await JsonHelper.AssertDataDumped(srcCn, dumpCn);
        }
        await PostgresHelper.CleanupTables(restoreCn, TablesToDump);
        ms.Seek(0, SeekOrigin.Begin);
        using (var dumpCn = new Utf8JsonAsyncStreamReader(ms, leaveOpen: true))
        {
            await JsonHelper.RestoreDataFromJson(restoreCn, dumpCn, tables, RestoreDataFromJson);
        }
        
        //Assert
        ms.Seek(0, SeekOrigin.Begin);
        using (var dumpCn = new Utf8JsonAsyncStreamReader(ms, leaveOpen: true))
        {
            await JsonHelper.AssertDataRestored(restoreCn, dumpCn);
        }
        await tran.CommitAsync();
    }

    private NpgsqlConnection GetPostgresConnection(string connectionString) => new(connectionString);

    /// <summary>
    /// Пишет данные в Json
    /// </summary>
    private async Task<PgTable> DumpDataToJson(NpgsqlConnection postgresCn, Utf8JsonWriter jsonCn, string tableName)
    {
        var table = await postgresCn.GetTableInfo([tableName]);
        await using var reader = PostgresHelper.GetReader(postgresCn, table!);
        await jsonCn.Write(table!, async writer => await writer.WriteFrom(reader));
        return table!;
    }
    
    /// <summary>
    /// Восстанавливает данные из Json
    /// </summary>
    private async Task RestoreDataFromJson(NpgsqlConnection postgresCn, IUtf8JsonAsyncStreamReader jsonCn, PgTable table)
    {
        await using var reader = jsonCn.GetRowReader(RowState.AddedOrModified, ownsReader: false);
        await postgresCn.BulkWrite(table, async writer => await writer.WriteFrom(reader));
    }
}