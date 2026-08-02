using System.IO.Compression;
using CsvHelper;
using Npgsql;
using ThisShard.Database.Core.Extensions;
using ThisShard.Database.Core.Models.Rows;
using ThisShard.Database.Infrastructure.Extensions;
using ThisShard.Database.Infrastructure.Postgres.Models;
using ThisShard.Database.Tests.Helpers;

namespace ThisShard.Database.Tests.Tests.Dump;

public class CsvDumpTests
{
    private const string PgConnectionString =
        "Host=localhost;port=5432;Database=kdb;Username=postgres;Password=postgres;Include Error Detail=true;";
    private const string RestoreConnectionString =
        "Host=localhost;port=5432;Database=kdbtest;Username=postgres;Password=postgres;Include Error Detail=true;";
    private const string CsvOutputFileName = "/Users/shard/Documents/dump.zip";

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
        using (var dumpCn = new ZipArchive(ms, ZipArchiveMode.Create, true))
        {
            await Helpers.CsvHelper.DumpDataToCsv(srcCn, dumpCn, TablesToDump, DumpDataToCsv);
        }
        
        //Assert
        ms.Seek(0, SeekOrigin.Begin);
        using (var assertCn = new ZipArchive(ms, ZipArchiveMode.Read, true))
        {
            await Helpers.CsvHelper.AssertDataDumped(srcCn, assertCn);
        }
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
        using (var dumpCn = new ZipArchive(ms, ZipArchiveMode.Create, true))
        {
            tables = await Helpers.CsvHelper.DumpDataToCsv(srcCn, dumpCn, TablesToDump, DumpDataToCsv);
        }
        ms.Seek(0, SeekOrigin.Begin);
        using (var dumpCn = new ZipArchive(ms, ZipArchiveMode.Read, true))
        {
            await Helpers.CsvHelper.AssertDataDumped(srcCn, dumpCn);
        }
        await PostgresHelper.CleanupTables(restoreCn, TablesToDump);
        ms.Seek(0, SeekOrigin.Begin);
        using (var dumpCn = new ZipArchive(ms, ZipArchiveMode.Read, true))
        {
            await Helpers.CsvHelper.RestoreDataFromCsv(restoreCn, dumpCn, tables, RestoreDataFromJson);
        }
        
        //Assert
        ms.Seek(0, SeekOrigin.Begin);
        using (var dumpCn = new ZipArchive(ms, ZipArchiveMode.Read, true))
        {
            await Helpers.CsvHelper.AssertDataRestored(restoreCn, dumpCn);
        }
        await tran.CommitAsync();
    }

    private NpgsqlConnection GetPostgresConnection(string connectionString) => new(connectionString);

    /// <summary>
    /// Пишет данные в Csv
    /// </summary>
    private async Task<PgTable> DumpDataToCsv(NpgsqlConnection postgresCn, CsvWriter csvCn, string tableName)
    {
        var table = await postgresCn.GetTableInfo([tableName]);
        await using var reader = PostgresHelper.GetReader(postgresCn, table!);
        await csvCn.Write(table!, async writer => await writer.WriteFrom(reader));
        return table!;
    }
    
    /// <summary>
    /// Восстанавливает данные из Csv
    /// </summary>
    private async Task RestoreDataFromJson(NpgsqlConnection postgresCn, CsvReader csvCn, PgTable table)
    {
        await using var reader = csvCn.GetRowReader(RowState.AddedOrModified, ownsReader: false);
        await postgresCn.BulkWrite(table, async writer => await writer.WriteFrom(reader));
    }
}