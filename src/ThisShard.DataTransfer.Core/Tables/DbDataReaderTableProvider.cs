using System.Data;
using System.Data.Common;
using ThisShard.Database.Core.Models;
using ThisShard.Database.Core.Models.Columns;
using ThisShard.Database.Core.Models.Tables;

namespace ThisShard.Database.Core.Tables;

/// <summary>
/// Провайдер таблицы из DbDataReader
/// </summary>
public class DbDataReaderTableProvider : IDbDataReaderTableProvider
{
    /// <summary>
    /// Резолвер ключа таблицы
    /// </summary>
    public static Func<string, string> DefaultTableKeyResolver { get; set; } = key => key;

    /// <summary>
    /// Резолвер ключа столбца по умолчанию
    /// </summary>
    public static Func<string, string, string> DefaultColumnKeyResolver { get; set; } = (_, key) => key;
    
    private readonly Func<string, string> _tableKeyResolver;
    private readonly Func<string,string,string> _columnKeyResolver;
    
    public DbDataReaderTableProvider() : this(DefaultTableKeyResolver, DefaultColumnKeyResolver)
    {
    }
    
    public DbDataReaderTableProvider(Func<string, string> tableKeyResolver, Func<string, string, string> columnKeyResolver)
    {
        _tableKeyResolver = tableKeyResolver ?? throw new ArgumentNullException(nameof(tableKeyResolver));
        _columnKeyResolver = columnKeyResolver ?? throw new ArgumentNullException(nameof(columnKeyResolver));
    }

    /// <summary>
    /// Возвращает схему таблицы для указанного пути
    /// </summary>
    public async Task<Table?> GetTable(DbDataReader reader, string[] path)
    {
        var tablePath = string.Join(".", path);
        
        var schemaTable = await reader.GetSchemaTableAsync();
        if (schemaTable == null)
            return null;

        var tableKey = _tableKeyResolver(string.Join(".", path));
        
        var columns = GetColumns(schemaTable, tableKey).ToList();

        return new Table
        {
            Key = tableKey,
            Path = tablePath,
            RawPath = path,
            Columns = columns,
        };
    }

    /// <summary>
    /// Возвращает колонки из таблицы схемы
    /// </summary>
    private IEnumerable<Column> GetColumns(DataTable schemaTable, string tableKey)
    {
        foreach (DataRow row in schemaTable.Rows)
        {
            var name = row.Field<string>("ColumnName")!;
            var type = row.Field<Type>("DataType")!;
            var isKey = row.Field<bool>("IsKey");
            var isReadOnly = row.Field<bool>("IsReadOnly");
            var isNullable = row.Field<bool?>("AllowDBNull");
            
            var key = _columnKeyResolver(tableKey, name);
            
            yield return new Column
            {
                Key = key,
                Path = name,
                RawName = name,
                IsPrimaryKey = isKey,
                IsReadOnly = isReadOnly,
                Type = type,
                IsNullable = isNullable ?? true,
            };
        }
    }
}