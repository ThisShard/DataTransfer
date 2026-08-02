using System.Data;
using ThisShard.Database.Core.Models;
using ThisShard.Database.Core.Models.Rows;

namespace ThisShard.Database.Core.Extensions;

/// <summary>
/// Расширения для RowState
/// </summary>
public static class RowStateExtensions
{
    /// <summary>
    /// Конвертирует DataRowState в RowState
    /// </summary>
    public static RowState ToRowState(this DataRowState dataRowState)
    {
        return dataRowState switch
        {
            DataRowState.Added => RowState.Added,
            DataRowState.Modified => RowState.Modified,
            DataRowState.Deleted => RowState.Deleted,
            _ => RowState.Ignored
        };
    }

    /// <summary>
    /// Возвращает безопасный стейт строки без проверок
    /// </summary>
    public static RowState GetSafeState(this RowState rowState)
    {
        return rowState switch
        {
            RowState.Added => RowState.AddedOrModified,
            RowState.Modified => RowState.AddedOrModified,
            RowState.Deleted => RowState.SafeDeleted,
            _ => rowState
        };
    }
}