using System.Data.Common;
using ThisShard.Database.Core.Models;
using ThisShard.Database.Core.Models.Rows;
using ThisShard.Database.Core.Models.Tables;
using ThisShard.Database.Core.Options;
using ThisShard.Database.Core.Readers;

namespace ThisShard.Database.Core.Extensions;

public static class DbDataReaderExtensions
{
    /// <summary>
    /// Возвращает читателя строк для DbDataReader
    /// </summary>
    public static DbRowReader GetRowReader(this DbDataReader reader, RowState defaultRowState = RowState.Added, bool ownsReader = true)
    {
        return new DbRowReader(reader, defaultRowState, ownsReader);
    }

    /// <summary>
    /// Возвращает читателя строк для DbDataReader
    /// </summary>
    public static async Task<DbRowReader> GetRowReader<TReader>(this Task<TReader> task,
        RowState defaultRowState = RowState.Added)
        where TReader : DbDataReader
    {
        return (await task).GetRowReader(defaultRowState);
    }

    /// <summary>
    /// Возвращает таблицу из DbDataReader
    /// </summary>
    public static async Task<ITable?> GetBulkOperationsTable(this DbDataReader reader, string[] tablePath, BulkOperationsOptions? options = null)
    {
        options ??= BulkOperationsOptions.Default;

        return await options.DbDataReaderTableProvider.GetTable(reader, tablePath);
    }
}