using System.IO.Compression;
using System.Text.Json;
using System.Text.Json.Stream;
using Npgsql;
using ThisShard.Database.Core.Pipelines.Builders;
using ThisShard.Database.Infrastructure.Extensions;
using ThisShard.Database.Tests.Helpers;

namespace ThisShard.Database.Tests.Tests.Pipeline;

public class JsonPipelineTests : BasePipelineTests<MultiStream>
{
    protected override ValueTask<MultiStream> GetConnection() => ValueTask.FromResult(new MultiStream());

    protected override async ValueTask AssertDataDumped(NpgsqlConnection srcCn, MultiStream dumpCn, string[] tableNames)
    {
        foreach (var tableName in tableNames)
        {
            var stream = dumpCn.GetStreamAtBeginning(tableName);
            using var reader = new Utf8JsonAsyncStreamReader(stream, leaveOpen: true);
            await JsonHelper.AssertDataDumped(srcCn, reader, tableName);
        }
    }

    protected override async ValueTask AssertDataRestored(NpgsqlConnection srcCn, MultiStream dumpCn, string[] tableNames)
    {
        foreach (var tableName in tableNames)
        {
            var stream = dumpCn.GetStreamAtBeginning(tableName);
            using var reader = new Utf8JsonAsyncStreamReader(stream, leaveOpen: true);
            await JsonHelper.AssertDataRestored(srcCn, reader, tableName);
        }
    }

    protected override void AddDestination(IPipelineBuilder pipelineBuilder, MultiStream dumpConnection, string tableName)
    {
        var stream = dumpConnection.GetStreamAtBeginning(tableName);
        
        pipelineBuilder.AddJsonDestination(c => c
            .WithKey(tableName)
            .WithWriterFactory(() => ValueTask.FromResult(new Utf8JsonWriter(stream)))
        );
    }

    protected override void AddSource(IPipelineBuilder pipelineBuilder, MultiStream dumpConnection, string tableName)
    {
        var stream = dumpConnection.GetStreamAtBeginning(tableName);
        
        pipelineBuilder.AddJsonSource(c => c
            .WithKey(tableName)
            .WithReaderFactory(() => ValueTask.FromResult<IUtf8JsonAsyncStreamReader>(new Utf8JsonAsyncStreamReader(stream, leaveOpen: true)))
        );
    }
}