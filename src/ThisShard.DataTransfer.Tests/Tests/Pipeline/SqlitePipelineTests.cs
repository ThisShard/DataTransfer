using Microsoft.Data.Sqlite;
using Npgsql;
using ThisShard.Database.Core.Extensions;
using ThisShard.Database.Core.Models.States;
using ThisShard.Database.Core.Pipelines;
using ThisShard.Database.Core.Pipelines.Builders;
using ThisShard.Database.Infrastructure.Extensions;
using ThisShard.Database.Tests.Helpers;

namespace ThisShard.Database.Tests.Tests.Pipeline;

public class SqlitePipelineTests : BasePipelineTests<SqliteConnection>
{
    private const string SqliteConnectionString =
        //"Data Source=/Users/shard/Documents/dump.db";
        "Data Source=:memory:";
    
    protected override async ValueTask<SqliteConnection> GetConnection()
    {
        var connection = new SqliteConnection(SqliteConnectionString);
        await connection.OpenAsync();
        return connection;
    }

    protected override async ValueTask AssertDataDumped(NpgsqlConnection srcCn, SqliteConnection dumpCn, string[] tableNames)
    {
        await DbHelper.AssertDataDumped(srcCn, dumpCn, tableNames);
    }

    protected override async ValueTask AssertDataRestored(NpgsqlConnection srcCn, SqliteConnection dumpCn, string[] tableNames)
    {
        await DbHelper.AssertDataRestored(srcCn, dumpCn, tableNames);
    }

    protected override void AddDestination(IPipelineBuilder pipelineBuilder, SqliteConnection dumpConnection, string tableName)
    {
        pipelineBuilder
            .AddSqliteDestination(c => c
                .WithKey(tableName)
                .WithTable(tableName)
                .WithConnection(dumpConnection)
                .CreateTableIfNotExists()
            );
    }

    protected override void AddSource(IPipelineBuilder pipelineBuilder, SqliteConnection dumpConnection, string tableName)
    {
        pipelineBuilder
            .AddSqliteSource(c => c
                .WithKey(tableName)
                .WithTable(tableName)
                .WithConnection(dumpConnection)
            );
    }
}