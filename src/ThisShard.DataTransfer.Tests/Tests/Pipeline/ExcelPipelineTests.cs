using System.IO.Compression;
using ExcelDataReader;
using LargeXlsx;
using Npgsql;
using ThisShard.Database.Core.Pipelines.Builders;
using ThisShard.Database.Infrastructure.Extensions;
using ThisShard.Database.Tests.Helpers;

namespace ThisShard.Database.Tests.Tests.Pipeline;

public class ExcelPipelineTests : BasePipelineTests<MultiStream>
{
    static ExcelPipelineTests()
    {
        System.Text.Encoding.RegisterProvider(System.Text.CodePagesEncodingProvider.Instance);
    }

    protected override ValueTask<MultiStream> GetConnection() => ValueTask.FromResult(new MultiStream());

    protected override async ValueTask AssertDataDumped(NpgsqlConnection srcCn, MultiStream dumpCn, string[] tableNames)
    {
        foreach (var tableName in tableNames)
        {
            var stream = dumpCn.GetStreamAtBeginning(tableName);
            using var reader = ExcelReaderFactory.CreateReader(stream, new ExcelReaderConfiguration(){LeaveOpen = true});
            await ExcelHelper.AssertDataDumped(srcCn, reader, tableName);
        }
    }

    protected override async ValueTask AssertDataRestored(NpgsqlConnection srcCn, MultiStream dumpCn, string[] tableNames)
    {
        foreach (var tableName in tableNames)
        {
            var stream = dumpCn.GetStreamAtBeginning(tableName);
            using var reader = ExcelReaderFactory.CreateReader(stream, new ExcelReaderConfiguration(){LeaveOpen = true});
            await ExcelHelper.AssertDataRestored(srcCn, reader, tableName);
        }
    }

    protected override void AddDestination(IPipelineBuilder pipelineBuilder, MultiStream dumpConnection, string tableName)
    {
        var stream = dumpConnection.GetStreamAtBeginning(tableName);
        
        pipelineBuilder.AddExcelDestination(c => c
            .WithKey(tableName)
            .WithWriterFactory(async () =>
            {
                var writer = new XlsxWriter(stream);
                await writer.BeginWorksheetAsync(tableName);
                return writer;
            })
        );
    }

    protected override void AddSource(IPipelineBuilder pipelineBuilder, MultiStream dumpConnection, string tableName)
    {
        var stream = dumpConnection.GetStreamAtBeginning(tableName);
        
        pipelineBuilder.AddExcelSource(c => c
            .WithKey(tableName)
            .WithReaderFactory(() => ValueTask.FromResult(ExcelReaderFactory.CreateReader(stream, new ExcelReaderConfiguration(){LeaveOpen = true})))
        );
    }
}