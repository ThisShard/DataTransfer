using System.Globalization;
using CsvHelper;
using Npgsql;
using ThisShard.Database.Core.Pipelines.Builders;
using ThisShard.Database.Infrastructure.Extensions;
using ThisShard.Database.Tests.Helpers;

namespace ThisShard.Database.Tests.Tests.Pipeline;

public class CsvPipelineTests : BasePipelineTests<MultiStream>
{
    protected override ValueTask<MultiStream> GetConnection() => ValueTask.FromResult(new MultiStream());

    protected override async ValueTask AssertDataDumped(NpgsqlConnection srcCn, MultiStream dumpCn, string[] tableNames)
    {
        foreach (var tableName in tableNames)
        {
            var stream = dumpCn.GetStreamAtBeginning(tableName);
            using var reader = new CsvReader(new StreamReader(stream, leaveOpen: true), CultureInfo.InvariantCulture, leaveOpen: true);
            await Helpers.CsvHelper.AssertDataDumped(srcCn, reader, tableName);
        }
    }

    protected override async ValueTask AssertDataRestored(NpgsqlConnection srcCn, MultiStream dumpCn, string[] tableNames)
    {
        foreach (var tableName in tableNames)
        {
            var stream = dumpCn.GetStreamAtBeginning(tableName);
            using var reader = new CsvReader(new StreamReader(stream, leaveOpen: true), CultureInfo.InvariantCulture, leaveOpen: true);
            await Helpers.CsvHelper.AssertDataRestored(srcCn, reader, tableName);
        }
    }

    protected override void AddDestination(IPipelineBuilder pipelineBuilder, MultiStream dumpConnection, string tableName)
    {
        var stream = dumpConnection.GetStreamAtBeginning(tableName);
        
        pipelineBuilder.AddCsvDestination(c => c
            .WithKey(tableName)
            .WithWriterFactory(() => ValueTask.FromResult(new CsvWriter(new StreamWriter(stream, leaveOpen: true), CultureInfo.InvariantCulture, leaveOpen:true)))
        );
    }

    protected override void AddSource(IPipelineBuilder pipelineBuilder, MultiStream dumpConnection, string tableName)
    {
        var stream = dumpConnection.GetStreamAtBeginning(tableName);
        
        pipelineBuilder.AddCsvSource(c => c
            .WithKey(tableName)
            .WithReaderFactory(() => ValueTask.FromResult(new CsvReader(new StreamReader(stream, leaveOpen: true), CultureInfo.InvariantCulture, leaveOpen: true)))
        );
    }
}