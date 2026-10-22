using System.Data.Common;
using ThisShard.Database.Models.Rows;
using ThisShard.Database.Models.Tables;
using ThisShard.Database.Options;
using ThisShard.Database.Readers;

namespace ThisShard.Database.Extensions;

public static class DbDataReaderExtensions
{
    /// <summary>
    /// Возвращает читателя строк для DbDataReader
    /// </summary>
    public static IReader GetReader(this DbDataReader reader, RowState defaultRowState = RowState.Added, bool ownsReader = true)
    {
        return new DbDataReaderAdapter(reader, defaultRowState, ownsReader);
    }

    /// <summary>
    /// Возвращает читателя строк для DbDataReader
    /// </summary>
    public static async Task<IReader> GetReader<TReader>(this Task<TReader> task,
        RowState defaultRowState = RowState.Added)
        where TReader : DbDataReader
    {
        return (await task).GetReader(defaultRowState);
    }

    /// <summary>
    /// Возвращает таблицу из DbDataReader
    /// </summary>
    public static async Task<ITable?> GetTable(this DbDataReader reader, string[] tablePath, DbDataReaderOptions? options = null)
    {
        options ??= DbDataReaderOptions.Default;

        return await options.DbDataReaderTableProvider.GetTable(reader, tablePath);
    }
}