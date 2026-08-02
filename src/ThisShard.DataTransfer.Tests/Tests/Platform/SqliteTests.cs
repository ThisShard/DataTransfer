using Microsoft.Data.Sqlite;
using ThisShard.Database.Core.Extensions;
using ThisShard.Database.Core.Models.Filters;
using ThisShard.Database.Core.Models.Rows;
using ThisShard.Database.Core.Options;
using ThisShard.Database.Infrastructure.Extensions;
using ThisShard.Database.Infrastructure.Sqlite.Options;
using ThisShard.Database.Tests.Helpers;

namespace ThisShard.Database.Tests.Tests.Platform;

public class SqliteTests
{
    private const string ConnectionString =
        "Data Source=:memory:";
    
    [Theory]
    [InlineData(1)]
    [InlineData(10)]
    [InlineData(100)]
    [InlineData(1000)]
    [InlineData(10000)]
    [InlineData(100000)]
    public async Task BulkOperationsTest(int count)
    {
        //Arrange
        await using var cn = GetConnection();
        await cn.OpenAsync();
        
        var tableName = $"bulk_operations_test_{count}";
        async Task TestCase(RowState rowState) => await cn.Write(tableName, async writer => await writer.Write(SqliteHelper.GenerateTestRows(rowState, count)));
        
        //Act
        await SqliteHelper.UsingTestTable(cn, tableName, async () =>
        {
            await TestCase(RowState.Added);
            await TestCase(RowState.Modified);
            await TestCase(RowState.Deleted);
            await TestCase(RowState.SafeDeleted);
            await TestCase(RowState.AddedOrModified);
            await TestCase(RowState.AddedOrModified);
            await TestCase(RowState.SafeDeleted);
            await TestCase(RowState.SafeDeleted);
        });
    }

    [Theory]
    [InlineData(100)]
    [InlineData(10000)]
    public async Task FilteringSourceWriteTest(int count)
    {
        //Arrange
        await using var cn = GetConnection();
        await cn.OpenAsync();
        
        var tableName = $"filtering_operations_test_{count}";
        var filterOptions = new SqliteBulkOperationsOptions()
        {
            CommandFilterFactory = (ctx) => new CommandFilter<SqliteParameter>()
            {
                CommandFilterText = $"{ctx.GetSourceColumnPath("id")} > @test_id",
                Parameters = new[]
                {
                    () => new SqliteParameter("@test_id", count / 2),
                }
            }
        };
        
        async Task TestCase(RowState rowState, SqliteBulkOperationsOptions? options = null) => 
            await cn.Write(tableName, async writer => await writer.Write(SqliteHelper.GenerateTestRows(rowState, count)), options);
        
        //Act
        await SqliteHelper.UsingTestTable(cn, tableName, async () =>
        {
            await TestCase(RowState.Added);
            await TestCase(RowState.Modified, filterOptions);
            
            var rows = await SqliteHelper.GetRows(cn, tableName);
            Assert.Equal(count / 2, GetFilteredRows(rows, name => name.Contains(nameof(RowState.Modified))).Count());
            
            await TestCase(RowState.AddedOrModified, filterOptions);
            
            rows = await SqliteHelper.GetRows(cn, tableName);
            Assert.Equal(count / 2, GetFilteredRows(rows, name => name.Contains(nameof(RowState.Modified))).Count());
            
            await TestCase(RowState.Deleted, filterOptions);
            
            rows = await SqliteHelper.GetRows(cn, tableName);
            Assert.Equal(count / 2, rows.Count);
        });
    }

    [Theory]
    [InlineData(100)]
    [InlineData(10000)]
    public async Task FilteringTargetWriteTest(int count)
    {
        //Arrange
        await using var cn = GetConnection();
        await cn.OpenAsync();
        
        var tableName = $"filtering_operations_test_{count}";
        var filterOptions = new SqliteBulkOperationsOptions()
        {
            CommandFilterFactory = (ctx) => new CommandFilter<SqliteParameter>()
            {
                CommandFilterText = $"{ctx.GetTargetColumnPath("id")} > @test_id",
                Parameters = new[]
                {
                    () => new SqliteParameter("@test_id", count / 2),
                }
            }
        };
        
        async Task TestCase(RowState rowState, SqliteBulkOperationsOptions? options = null) => 
            await cn.Write(tableName, async writer => await writer.Write(SqliteHelper.GenerateTestRows(rowState, count)), options);
        
        //Act
        await SqliteHelper.UsingTestTable(cn, tableName, async () =>
        {
            await TestCase(RowState.Added);
            await TestCase(RowState.Modified, filterOptions);
            
            var rows = await SqliteHelper.GetRows(cn, tableName);
            Assert.Equal(count / 2, GetFilteredRows(rows, name => name.Contains(nameof(RowState.Modified))).Count());
            
            await TestCase(RowState.AddedOrModified, filterOptions);
            
            rows = await SqliteHelper.GetRows(cn, tableName);
            Assert.Equal(count / 2, GetFilteredRows(rows, name => name.Contains(nameof(RowState.Modified))).Count());
            
            await TestCase(RowState.Deleted, filterOptions);
            
            rows = await SqliteHelper.GetRows(cn, tableName);
            Assert.Equal(count / 2, rows.Count);
        });
    }

    private IEnumerable<IRow> GetFilteredRows(IEnumerable<IRow> rows, Func<string, bool> nameFilter)
    {
        return rows.Where(x => x.TryGetValue("date", out var name) && name is string str && nameFilter(str));
    }
    
    [Theory]
    [InlineData(100)]
    [InlineData(10000)]
    public async Task SustainableTest(int count)
    {
        //Arrange
        await using var cn = GetConnection();
        await cn.OpenAsync();
        
        var tableName = $"sustainable_operations_test_{count}";

        async Task TestCase(RowState rowState) => await cn.Write(tableName,
            async writer => await writer.Write(SqliteHelper.GenerateTestRows(rowState, count)),
            new SqliteBulkOperationsOptions() { SustainableOptions = SustainableOperationsOptions<SqliteConnection>.Default });
        
        //Act
        await SqliteHelper.UsingTestTable(cn, tableName, async () =>
        {
            await TestCase(RowState.Added);
            await TestCase(RowState.Modified);
            await TestCase(RowState.Deleted);
            await TestCase(RowState.SafeDeleted);
            await TestCase(RowState.AddedOrModified);
            await TestCase(RowState.AddedOrModified);
            await TestCase(RowState.SafeDeleted);
            await TestCase(RowState.SafeDeleted);
        });
    }
    
    [Theory]
    [InlineData(100)]
    [InlineData(10000)]
    public async Task TransactionTest(int count)
    {
        //Arrange
        await using var cn = GetConnection();
        await cn.OpenAsync();
        await using var tran = await cn.BeginTransactionAsync();
        
        var tableName = $"transaction_test_{count}";
        async Task TestCase(RowState rowState) => await cn.Write(tableName, async writer => await writer.Write(SqliteHelper.GenerateTestRows(rowState, count)));
        
        //Act
        await SqliteHelper.UsingTestTable(cn, tableName, async () =>
        {
            await TestCase(RowState.Added);
        });
        await tran.CommitAsync();
    }
    
    [Theory]
    [InlineData(100)]
    [InlineData(10000)]
    public async Task TransactionFailedTest(int count)
    {
        //Arrange
        await using var cn = GetConnection();
        await cn.OpenAsync();
        
        var tableName = $"transaction_test_{count}";
        async Task TestCase(RowState rowState) => await cn.Write(tableName, async writer => await writer.Write(SqliteHelper.GenerateTestRows(rowState, count)));
        
        //Act
        var exception = await Assert.ThrowsAsync<SqliteException>(async () =>
        {
            await SqliteHelper.UsingTestTable(cn, tableName, async () =>
            {
                await using var tran = await cn.BeginTransactionAsync();
                await TestCase(RowState.Added);
                await TestCase(RowState.Added);
            });
        });
        Assert.Equal(19, exception.SqliteErrorCode);
    }

    [Theory]
    [InlineData(100)]
    [InlineData(10000)]
    public async Task ObjectsTest(int count)
    {
        //Arrange
        await using var cn = GetConnection();
        await cn.OpenAsync();
        await using var tran = await cn.BeginTransactionAsync();
        
        var tableName = $"transaction_test_{count}";

        async Task TestCase(RowState rowState) => await cn.Write(tableName, async writer =>
            await writer.Write(Enumerable.Range(1, count).Select(x => new
            {
                id = x,
                date = $"test_{rowState}_{x}"
            }), rowState));
        
        //Act
        await SqliteHelper.UsingTestTable(cn, tableName, async () =>
        {
            await TestCase(RowState.Added);
        });
        await tran.CommitAsync();
    }

    [Theory]
    [InlineData(100)]
    public async Task CopyDataTest(int count)
    {
        //Arrange
        await using var readCn = GetConnection();
        await using var writeCn = GetConnection();
        await readCn.OpenAsync();
        await writeCn.OpenAsync();
        
        var readTable = $"copy_from_{count}";
        var writeTable = $"copy_to_{count}";
        
        await SqliteHelper.UsingTestTable(readCn, readTable, async () =>
        {
            await readCn.Write(readTable, async writer => await writer.Write(SqliteHelper.GenerateTestRows(RowState.Added,count)));
            
            //Act
            await SqliteHelper.UsingTestTable(writeCn, writeTable, async () =>
            {
                await using var readCmd = readCn.CreateCommand();
                readCmd.CommandText = $"SELECT * FROM {readTable}";
                await using var reader = await readCmd.ExecuteReaderAsync();
                await writeCn.Write(writeTable, async writer => await writer.WriteFrom(reader));
                
                //Assert
                await using var assertCmd = writeCn.CreateCommand();
                assertCmd.CommandText = $"SELECT * FROM {writeTable}";
                await using var assertReader = await assertCmd.ExecuteReaderAsync().GetRowReader();
                var rows = await assertReader.ReadToEnd();
                Assert.Equal(count, rows.Count);
            });
        });
    }
    
    private SqliteConnection GetConnection() => new(ConnectionString);
}