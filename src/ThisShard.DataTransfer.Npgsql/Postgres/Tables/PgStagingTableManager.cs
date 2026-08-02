using Npgsql;
using ThisShard.Database.Core.Models;
using ThisShard.Database.Core.Models.Columns;
using ThisShard.Database.Infrastructure.Postgres.Helpers;
using ThisShard.Database.Infrastructure.Postgres.Models;

namespace ThisShard.Database.Infrastructure.Postgres.Tables;

/// <summary>
/// Менеджер временных таблиц
/// </summary>
public class PgStagingTableManager : IPgStagingTableManager
{
    /// <summary>
    /// Резолвер имени временной таблицы по умолчанию
    /// </summary>
    public static Func<string, string> DefaultTableNameResolver { get; set; } = 
        _ => $"STG_{Guid.NewGuid()}";

    /// <summary>
    /// Резолвер имени столбца по умолчанию
    /// </summary>
    public static Func<string?, StagingColumnType, int, string> DefaultColumnNameResolver { get; set; } =
        (_, _, index) => $"col_{index}";

    /// <summary>
    /// Резолвер имени индекса по умолчанию
    /// </summary>
    public static Func<string, string> DefaultIndexNameResolver { get; set; } =
        table => $"IX_{table}";
    
    private const string BatchIdColumnKey = "__BATCH_ID__";
    private const string RowStateColumnKey = "__ROW_STATE__";
    
    private const string BoolDataType = "bool";
    private const string GuidDataType = "uuid";
    private const string IntDataType = "int4";
    
    private readonly Func<string, string> _tableNameResolver;
    private readonly Func<string?, StagingColumnType, int, string> _columnNameResolver;
    private readonly Func<string, string> _indexNameResolver;

    public PgStagingTableManager() : this(DefaultTableNameResolver, DefaultColumnNameResolver,
        DefaultIndexNameResolver)
    {
    }
    
    public PgStagingTableManager(Func<string, string> tableNameResolver, Func<string?, StagingColumnType, int, string> columnNameResolver, Func<string, string> indexNameResolver)
    {
        _tableNameResolver = tableNameResolver ?? throw new ArgumentNullException(nameof(tableNameResolver));
        _columnNameResolver = columnNameResolver ?? throw new ArgumentNullException(nameof(columnNameResolver));
        _indexNameResolver = indexNameResolver ?? throw new ArgumentNullException(nameof(indexNameResolver));
    }

    /// <summary>
    /// Создать временную таблицу
    /// </summary>
    public async Task<PgStagingTable> CreateStagingTable(NpgsqlConnection connection, PgTable table)
    {
        var stagingTable = BuildStagingTable(table);
        await CreateStagingTable(connection, stagingTable);
        return stagingTable;
    }

    /// <summary>
    /// Создать временную таблицу
    /// </summary>
    public async Task CreateStagingTable(NpgsqlConnection connection, PgStagingTable stagingTable)
    {
        await using var command = connection.CreateCommand();

        var columnsSql = string.Join(", ", stagingTable.Columns.Select(col => $"{col.Path} {col.DataTypeName}"));
        
        PgStagingColumn[] indexColumns =
        [
            stagingTable.Columns.First(x => x.StagingColumnType == StagingColumnType.BatchId),
            stagingTable.Columns.First(x => x.StagingColumnType == StagingColumnType.RowState),
            ..stagingTable.Columns
                .Where(x=>x.StagingColumnType == StagingColumnType.Data)
                .Where(x=>x.LinkedColumn!.IsPrimaryKey)
                .OrderBy(x=>x.LinkedColumn!.PrimaryKeyOrdinal)
        ];
        var indexSqls = indexColumns
            .Select(x => $"{x.Path} {(x.LinkedColumn?.PrimaryKeyDesc == true ? "DESC" : "")} {(x.LinkedColumn?.PrimaryKeyNullsFirst == true ? "NULLS FIRST" : "")}");
        var indexSql = string.Join(", ", indexSqls);
        
        command.CommandText = $"""
                               CREATE TEMP TABLE IF NOT EXISTS {stagingTable.Path} ({columnsSql});
                               CREATE INDEX {PostgresNameFormatter.EscapePath(_indexNameResolver(stagingTable.RawPath.Last()))} ON {stagingTable.Path} ({indexSql});
                               """;
        
        await command.ExecuteNonQueryAsync();
    }

    /// <summary>
    /// Удалить временную таблицу
    /// </summary>
    public async Task DeleteStagingTable(NpgsqlConnection connection, PgStagingTable table)
    {
        await using var command = connection.CreateCommand();
        
        command.CommandText = $"""
                               DROP INDEX IF EXISTS {PostgresNameFormatter.EscapePath(_indexNameResolver(table.RawPath.Last()))};
                               DROP TABLE IF EXISTS {table.Path};
                               """;
        
        await command.ExecuteNonQueryAsync();
    }

    #region Private

    /// <summary>
    /// Строит временную таблицу
    /// </summary>
    private PgStagingTable BuildStagingTable(PgTable table)
    {
        string[] path = [_tableNameResolver(table.RawPath.Last())];

        return new PgStagingTable()
        {
            DestinationTable = table,
            Key = string.Join(".", path),
            RawPath = path,
            Path = PostgresNameFormatter.EscapePath(path),
            Columns = BuildColumns(table).ToArray()
        };
    }

    /// <summary>
    /// Строит колонки временной таблицы
    /// </summary>
    private IEnumerable<PgStagingColumn> BuildColumns(PgTable table)
    {
        var index = 0;
        
        var batchIdColumnName = _columnNameResolver(null, StagingColumnType.BatchId, index++);
        var rowStateColumnName = _columnNameResolver(null, StagingColumnType.RowState, index++);
        
        yield return new PgStagingColumn
        {
            StagingColumnType = StagingColumnType.BatchId,
            DataTypeName = GuidDataType,
            Key = BatchIdColumnKey,
            Type = typeof(Guid),
            Path = PostgresNameFormatter.EscapePath(batchIdColumnName),
            RawName = batchIdColumnName,
        };
        
        yield return new PgStagingColumn
        {
            StagingColumnType = StagingColumnType.RowState,
            DataTypeName = IntDataType,
            Key = RowStateColumnKey,
            Type = typeof(int),
            Path = PostgresNameFormatter.EscapePath(rowStateColumnName),
            RawName = rowStateColumnName,
        };
        
        foreach (var column in table.Columns)
        {
            var dataColumnName = _columnNameResolver(column.RawName,
                StagingColumnType.Data,
                index++);
            
            var flagColumnName = _columnNameResolver(column.RawName,
                StagingColumnType.DataModificationFlag,
                index++);
            
            yield return new PgStagingColumn
            {
                LinkedColumn = column,
                StagingColumnType = StagingColumnType.Data,
                DataTypeName = column.DataTypeName,
                Key = column.Key,
                Type = column.Type,
                Path = PostgresNameFormatter.EscapePath(dataColumnName),
                RawName = dataColumnName,
            };
            
            yield return new PgStagingColumn
            {
                LinkedColumn = column,
                StagingColumnType = StagingColumnType.DataModificationFlag,
                DataTypeName = BoolDataType,
                Key = column.Key,
                Type = typeof(bool),
                Path = PostgresNameFormatter.EscapePath(flagColumnName),
                RawName = flagColumnName,
            };
        }
    }

    #endregion
}