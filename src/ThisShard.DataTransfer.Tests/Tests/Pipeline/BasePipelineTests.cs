using Microsoft.Data.Sqlite;
using Npgsql;
using ThisShard.Database.Core.Extensions;
using ThisShard.Database.Core.Helpers;
using ThisShard.Database.Core.Models.States;
using ThisShard.Database.Core.Pipelines;
using ThisShard.Database.Core.Pipelines.Builders;
using ThisShard.Database.Infrastructure.Extensions;
using ThisShard.Database.Tests.Helpers;

namespace ThisShard.Database.Tests.Tests.Pipeline;

/// <summary>
/// Базовые тесты пайплайна
/// </summary>
public abstract class BasePipelineTests<TConnection>
{
    private const string DumpConnectionString =
        "Host=localhost;port=5432;Database=kdb;Username=postgres;Password=postgres;Include Error Detail=true;";
    private const string RestoreConnectionString =
        "Host=localhost;port=5432;Database=kdbtest;Username=postgres;Password=postgres;Include Error Detail=true;";
    
    private static readonly string[] TablesToDump =
    [
        "Configurations",
        //"Accounts",
        //"DictAbonents",
        //"Users",
        //"IdentityTokens",
        //"JournalTextTemplates",
        //"JournalRecords",
        //"Locks",
        //"Messages",
        //"NotificationTextTemplates",
        //"Notification",
        //"NotificationToUser",
    ];
    
    [Fact]
    public async Task DumpTest()
    {
        //Arrange
        var dumpCn = await GetConnection();
        try
        {
            await using var srcCn = GetPostgresConnection(DumpConnectionString);
            await srcCn.OpenAsync();
        
            //Act
            await ProcessPipeline(GetDumpPipeline(dumpCn));

            //Assert
            await AssertDataDumped(srcCn, dumpCn, TablesToDump);
        }
        finally
        {
            if (dumpCn is IAsyncDisposable asyncDisposable)
                await asyncDisposable.DisposeAsync();
            else if (dumpCn is IDisposable disposable)
                disposable.Dispose();
        }
    }

    [Fact]
    public async Task RestoreTest()
    {
        //Arrange
        var dumpCn = await GetConnection();
        try
        {
            await using var srcCn = GetPostgresConnection(DumpConnectionString);
            await using var restoreCn = GetPostgresConnection(RestoreConnectionString);
            await srcCn.OpenAsync();
            await restoreCn.OpenAsync();
        
            //Act
            await ProcessPipeline(GetDumpPipeline(dumpCn));
            await AssertDataDumped(restoreCn, dumpCn, TablesToDump);
            await PostgresHelper.CleanupTables(restoreCn, TablesToDump);
            await ProcessPipeline(GetRestorePipeline(dumpCn));
        
            //Assert
            await AssertDataRestored(restoreCn, dumpCn, TablesToDump);
        }
        finally
        {
            if (dumpCn is IAsyncDisposable asyncDisposable)
                await asyncDisposable.DisposeAsync();
            else if (dumpCn is IDisposable disposable)
                disposable.Dispose();
        }
    }

    private NpgsqlConnection GetPostgresConnection(string connectionString) => new(connectionString);

    protected abstract ValueTask<TConnection> GetConnection();
    
    protected abstract ValueTask AssertDataDumped(NpgsqlConnection srcCn, TConnection dumpCn, string[] tableNames);

    protected abstract ValueTask AssertDataRestored(NpgsqlConnection srcCn, TConnection dumpCn, string[] tableNames);

    /// <summary>
    /// Выполняет пайплайн
    /// </summary>
    private async Task ProcessPipeline(IPipeline<CompositePipelineState> pipeline)
    {
        await using (pipeline)
        {
            await pipeline.Start();
            await pipeline.Completion;
            if (pipeline.State!.Exception != null)
                throw pipeline.State.Exception;
        }
    }
    
    /// <summary>
    /// Возвращает пайплайн для дампа
    /// </summary>
    private IPipeline<CompositePipelineState> GetDumpPipeline(TConnection dumpConnection)
    {
        var builder = new CompositePipelineBuilder();

        foreach (var tableName in TablesToDump)
        {
            builder.AddPipeline(p =>
            {
                p
                    .WithKey(tableName)
                    .AddNpgsqlSource(c => c
                        .WithKey(tableName)
                        .WithTable([tableName])
                        .WithConnectionFactory(() => ValueTask.FromResult(GetPostgresConnection(DumpConnectionString)))
                    );
                
                AddDestination(p, dumpConnection, tableName);
            });
        }
        
        return builder.Build();
    }

    protected abstract void AddDestination(IPipelineBuilder pipelineBuilder, TConnection dumpConnection, string tableName);

    /// <summary>
    /// Возвращает пайплайн для рестора
    /// </summary>
    private IPipeline<CompositePipelineState> GetRestorePipeline(TConnection dumpConnection)
    {
        var builder = new CompositePipelineBuilder();

        foreach (var tableName in TablesToDump)
        {
            builder.AddPipeline(p =>
            {
                p
                    .WithKey(tableName)
                    .AddNpgsqlDestination(c => c
                        .WithKey(tableName)
                        .WithTable([tableName])
                        .WithConnectionFactory(() =>
                            ValueTask.FromResult(GetPostgresConnection(RestoreConnectionString)))
                    );
                
                AddSource(p, dumpConnection, tableName);
            });
        }
        
        return builder.Build();
    }

    protected abstract void AddSource(IPipelineBuilder pipelineBuilder, TConnection dumpConnection, string tableName);
}