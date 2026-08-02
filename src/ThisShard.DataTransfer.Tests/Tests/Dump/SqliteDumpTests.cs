using Microsoft.Data.Sqlite;
using Npgsql;
using ThisShard.Database.Core.Extensions;
using ThisShard.Database.Infrastructure.Extensions;
using ThisShard.Database.Infrastructure.Postgres.Models;
using ThisShard.Database.Tests.Helpers;

namespace ThisShard.Database.Tests.Tests.Dump;

public class SqliteDumpTests
{
    private const string PgConnectionString =
        "Host=localhost;port=5432;Database=kdb;Username=postgres;Password=postgres;Include Error Detail=true;";
    private const string SqliteConnectionString =
        //"Data Source=/Users/shard/Documents/dump.db";
        "Data Source=:memory:";

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
        await using var srcCn = GetPostgresConnection();
        await using var dumpCn = GetSqliteConnection();
        await srcCn.OpenAsync();
        await dumpCn.OpenAsync();
        
        //Act
        foreach (var tableName in TablesToDump)
        {
            await DumpDataToSqlite(srcCn, dumpCn, tableName);
        }

        //Assert
        await DbHelper.AssertDataDumped(srcCn, dumpCn, TablesToDump);
    }

    [Fact]
    public async Task RestoreTest()
    {
        //Arrange
        await using var srcCn = GetPostgresConnection();
        await using var dumpCn = GetSqliteConnection();
        await srcCn.OpenAsync();
        await dumpCn.OpenAsync();
        await using var tran = await srcCn.BeginTransactionAsync();
        
        //Act
        var tables = new List<PgTable>();
        foreach (var tableName in TablesToDump)
        {
            tables.Add(await DumpDataToSqlite(srcCn, dumpCn, tableName));
        }
        await DbHelper.AssertDataDumped(srcCn, dumpCn, TablesToDump);
        await PostgresHelper.CleanupTables(srcCn, TablesToDump);
        foreach (var table in tables)
        {
            await RestoreDataFromSqlite(srcCn, dumpCn, table);
        }
        
        //Assert
        await DbHelper.AssertDataRestored(srcCn, dumpCn, TablesToDump);
        await tran.CommitAsync();
    }

    private NpgsqlConnection GetPostgresConnection() => new(PgConnectionString);
    
    private SqliteConnection GetSqliteConnection() => new(SqliteConnectionString);

    /// <summary>
    /// Пишет данные в Sqlite
    /// </summary>
    private async Task<PgTable> DumpDataToSqlite(NpgsqlConnection postgresCn, SqliteConnection sqliteCn, string tableName)
    {
        var table = await postgresCn.GetTableInfo([tableName]);
        await using var reader = PostgresHelper.GetReader(postgresCn, table!);
        await sqliteCn.CreateTableAndWrite(table!, async writer => await writer.WriteFrom(reader));
        return table;
    }
    
    /// <summary>
    /// Восстанавливает данные из Sqlite
    /// </summary>
    private async Task RestoreDataFromSqlite(NpgsqlConnection postgresCn, SqliteConnection sqliteCn, PgTable table)
    {
        await using var reader = await SqliteHelper.GetReader(sqliteCn, table.RawPath.Last());
        await postgresCn.BulkWrite(table, async writer => await writer.WriteFrom(reader));
    }
}