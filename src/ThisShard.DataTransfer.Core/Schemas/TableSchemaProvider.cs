using ThisShard.Database.Core.Models;
using ThisShard.Database.Core.Models.Columns;
using ThisShard.Database.Core.Models.Tables;

namespace ThisShard.Database.Core.Schemas;

/// <summary>
/// Провайдер схем таблиц
/// </summary>
public class TableSchemaProvider : ITableSchemaProvider
{
    private readonly Func<IColumn, int, string> _createParameterName;

    public TableSchemaProvider(Func<IColumn, int, string> createParameterName)
    {
        _createParameterName = createParameterName ?? throw new ArgumentNullException(nameof(createParameterName));
    }

    /// <summary>
    /// Возвращает схему таблицы
    /// </summary>
    public ITableSchema GetSchema(ITable table)
    {
        var mutableColumns = table.Columns.Where(x => !x.IsReadOnly).ToArray();
        var primaryKeyColumns = table.Columns.Where(x => x.IsPrimaryKey).ToArray();
        var nonPrimaryKeyColumns = mutableColumns.Where(x => !x.IsPrimaryKey).ToArray();
        
        return new TableSchema()
        {
            Table = table,
            
            ColumnParameterMappings = table.Columns
                .Select((x, i) => (x, i))
                .ToDictionary(x => x.x, x => _createParameterName(x.x, x.i)),
        
            MutableColumns = mutableColumns,
            PrimaryKeyColumns = primaryKeyColumns,
            NonPrimaryKeyColumns = nonPrimaryKeyColumns,
        
            CanInsert = mutableColumns.Length > 0,
            CanUpdate = primaryKeyColumns.Length > 0 && nonPrimaryKeyColumns.Length > 0,
            CanDelete = primaryKeyColumns.Length > 0,
        };
    }
}