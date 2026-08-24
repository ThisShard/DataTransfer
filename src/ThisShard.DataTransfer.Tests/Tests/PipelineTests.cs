using Microsoft.Data.Sqlite;
using Npgsql;
using ThisShard.Database.Core.Extensions;
using ThisShard.Database.Core.Models.Rows;
using ThisShard.Database.Core.Models.States;
using ThisShard.Database.Core.Pipelines;
using ThisShard.Database.Core.Pipelines.Builders;
using ThisShard.Database.Infrastructure.Extensions;
using ThisShard.Database.Infrastructure.Postgres.Models;
using ThisShard.Database.Tests.Helpers;

namespace ThisShard.Database.Tests.Tests;

public class PipelineTests
{
    private const string DumpConnectionString =
        "Host=localhost;port=5432;Database=kdb;Username=postgres;Password=postgres;Include Error Detail=true;";
    private const string RestoreConnectionString =
        "Host=localhost;port=5432;Database=kdbtest;Username=postgres;Password=postgres;Include Error Detail=true;";
    private const string SqliteConnectionString =
        "Data Source=/Users/shard/Documents/dump.db";
        //"Data Source=:memory:";
    
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
        await using var srcCn = GetPostgresConnection(DumpConnectionString);
        await using var dumpCn = GetSqliteConnection();
        await srcCn.OpenAsync();
        await dumpCn.OpenAsync();
        
        //Act
        foreach (var tableName in TablesToDump)
        {
            await ProcessPipeline(GetDumpPipeline(tableName, dumpCn));
        }

        //Assert
        await DbHelper.AssertDataDumped(srcCn, dumpCn, TablesToDump);
    }

    [Fact]
    public async Task RestoreTest()
    {
        //Arrange
        await using var srcCn = GetPostgresConnection(DumpConnectionString);
        await using var restoreCn = GetPostgresConnection(RestoreConnectionString);
        await using var dumpCn = GetSqliteConnection();
        await srcCn.OpenAsync();
        await dumpCn.OpenAsync();
        await restoreCn.OpenAsync();
        
        //Act
        foreach (var tableName in TablesToDump)
        {
            await ProcessPipeline(GetDumpPipeline(tableName, dumpCn));
        }
        await DbHelper.AssertDataDumped(srcCn, dumpCn, TablesToDump);
        await PostgresHelper.CleanupTables(restoreCn, TablesToDump);
        foreach (var tableName in TablesToDump)
        {
            await ProcessPipeline(GetRestorePipeline(tableName, dumpCn));
        }
        
        //Assert
        await DbHelper.AssertDataRestored(restoreCn, dumpCn, TablesToDump);
    }

    private NpgsqlConnection GetPostgresConnection(string connectionString) => new(connectionString);

    private SqliteConnection GetSqliteConnection() => new(SqliteConnectionString);

    /// <summary>
    /// Выполняет пайплайн
    /// </summary>
    private async Task ProcessPipeline(IPipeline<PipelineState> pipeline)
    {
        await pipeline.Start();
        await pipeline.Completion;
        if (pipeline.State!.Exception != null)
            throw pipeline.State.Exception;
    }
    
    /// <summary>
    /// Возвращает пайплайн для дампа
    /// </summary>
    private IPipeline<PipelineState> GetDumpPipeline(string tableName, SqliteConnection dumpConnection)
    {
        var builder = new PipelineBuilder()
            .AddNpgsqlSource(c => c
                .WithTable([tableName])
                .WithConnectionFactory(() => ValueTask.FromResult(GetPostgresConnection(DumpConnectionString)))
            )
            .AddSqliteDestination(c => c
                .WithTable(tableName)
                .WithConnectionFactory(() => ValueTask.FromResult(dumpConnection), false)
                .CreateTableIfNotExists()
            );
        
        return builder.Build();
    }

    /// <summary>
    /// Возвращает пайплайн для рестора
    /// </summary>
    private IPipeline<PipelineState> GetRestorePipeline(string tableName, SqliteConnection dumpConnection)
    {
        var builder = new PipelineBuilder()
            .AddNpgsqlDestination(c => c
                .WithTable([tableName])
                .WithConnectionFactory(() => ValueTask.FromResult(GetPostgresConnection(RestoreConnectionString)))
            )
            .AddSqliteSource(c => c
                .WithTable(tableName)
                .WithConnectionFactory(() => ValueTask.FromResult(dumpConnection), false)
            );
        
        return builder.Build();
    }
}