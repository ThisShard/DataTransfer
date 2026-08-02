using ThisShard.Database.Core.Models;
using ThisShard.Database.Core.Models.Columns;
using ThisShard.Database.Core.Models.Tables;

namespace ThisShard.Database.Core.Schemas;

/// <summary>
/// Провайдер схем временных таблиц
/// </summary>
public class StagingTableSchemaProvider : IStagingTableSchemaProvider
{
    /// <summary>
    /// Возвращает схему временной таблицы
    /// </summary>
    public IStagingTableSchema GetSchema(IStagingTable table)
    {
        var columns = table.Columns.Where(x => x.LinkedColumn != null).GroupBy(x => x.LinkedColumn,
            (c, l) => new StagingColumnSchema()
            {
                DestinationColumn = c!,
                ValueColumn = l.First(x => x.StagingColumnType == StagingColumnType.Data),
                FlagColumn = l.First(x => x.StagingColumnType == StagingColumnType.DataModificationFlag)
            }).ToArray();

        var mutableColumns = columns.Where(x => !x.DestinationColumn.IsReadOnly).ToArray();
        var primaryKeyColumns = columns.Where(x => x.DestinationColumn.IsPrimaryKey).ToArray();
        var nonPrimaryKeyColumns = mutableColumns.Where(x => !x.DestinationColumn.IsPrimaryKey).ToArray();

        return new StagingTableSchema()
        {
            Table = table,
            
            BatchIdColumn = table.Columns.First(x => x.StagingColumnType == StagingColumnType.BatchId),
            RowStateColumn = table.Columns.First(x => x.StagingColumnType == StagingColumnType.RowState),
            StagingColumns = table.Columns
                .Where(x => x.StagingColumnType != StagingColumnType.Ignored)
                .ToArray(),

            Columns = columns,
            MutableColumns = mutableColumns,
            PrimaryKeyColumns = primaryKeyColumns,
            NonPrimaryKeyColumns = nonPrimaryKeyColumns,
        };
    }
}