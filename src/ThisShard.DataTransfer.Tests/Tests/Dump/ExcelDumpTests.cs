using ExcelDataReader;
using LargeXlsx;
using Npgsql;
using ThisShard.Database.Core.Extensions;
using ThisShard.Database.Core.Models.Rows;
using ThisShard.Database.Infrastructure.Extensions;
using ThisShard.Database.Infrastructure.Postgres.Models;
using ThisShard.Database.Tests.Helpers;

namespace ThisShard.Database.Tests.Tests.Dump;

public class ExcelDumpTests
{
    private const string PgConnectionString =
        "Host=localhost;port=5432;Database=kdb;Username=postgres;Password=postgres;Include Error Detail=true;";
    private const string RestoreConnectionString =
        "Host=localhost;port=5432;Database=kdbtest;Username=postgres;Password=postgres;Include Error Detail=true;";
    private const string ExcelOutputFileName = "/Users/shard/Documents/dump.xlsx";

    static ExcelDumpTests()
    {
        System.Text.Encoding.RegisterProvider(System.Text.CodePagesEncodingProvider.Instance);
    }
    
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
        await using (var dumpCn = new XlsxWriter(ms))
        {
            await ExcelHelper.DumpDataToExcel(srcCn, dumpCn, TablesToDump, DumpDataToExcel);
            await dumpCn.CommitAsync();
        }
        
        //Assert
        ms.Seek(0, SeekOrigin.Begin);
        using (var assertCn = ExcelReaderFactory.CreateReader(ms))
        {
            await ExcelHelper.AssertDataDumped(srcCn, assertCn);
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
        await using (var dumpCn = new XlsxWriter(ms))
        {
            tables = await ExcelHelper.DumpDataToExcel(srcCn, dumpCn, TablesToDump, DumpDataToExcel);
        }
        ms.Seek(0, SeekOrigin.Begin);
        using (var dumpCn = ExcelReaderFactory.CreateReader(ms, new ExcelReaderConfiguration(){LeaveOpen = true}))
        {
            await ExcelHelper.AssertDataDumped(srcCn, dumpCn);
        }
        await PostgresHelper.CleanupTables(restoreCn, TablesToDump);
        ms.Seek(0, SeekOrigin.Begin);
        using (var dumpCn = ExcelReaderFactory.CreateReader(ms, new ExcelReaderConfiguration(){LeaveOpen = true}))
        {
            await ExcelHelper.RestoreDataFromExcel(restoreCn, dumpCn, tables, RestoreDataFromJson);
        }
        
        //Assert
        ms.Seek(0, SeekOrigin.Begin);
        using (var dumpCn = ExcelReaderFactory.CreateReader(ms, new ExcelReaderConfiguration(){LeaveOpen = true}))
        {
            await ExcelHelper.AssertDataRestored(restoreCn, dumpCn);
        }
        await tran.CommitAsync();
    }

    private NpgsqlConnection GetPostgresConnection(string connectionString) => new(connectionString);

    /// <summary>
    /// Пишет данные в Excel
    /// </summary>
    private async Task<PgTable> DumpDataToExcel(NpgsqlConnection postgresCn, XlsxWriter excelCn, string tableName)
    {
        var table = await postgresCn.GetTableInfo([tableName]);
        await using var reader = PostgresHelper.GetReader(postgresCn, table!);
        await excelCn.Write(table!, async writer => await writer.WriteFrom(reader));
        return table!;
    }
    
    /// <summary>
    /// Восстанавливает данные из Excel
    /// </summary>
    private async Task RestoreDataFromJson(NpgsqlConnection postgresCn, IExcelDataReader excelCn, PgTable table)
    {
        await using var reader = excelCn.GetRowReader(RowState.AddedOrModified, ownsReader: false);
        await postgresCn.BulkWrite(table, async writer => await writer.WriteFrom(reader));
    }
}