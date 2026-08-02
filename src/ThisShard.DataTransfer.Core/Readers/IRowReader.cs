using ThisShard.Database.Core.Models;
using ThisShard.Database.Core.Models.Rows;

namespace ThisShard.Database.Core.Readers;

/// <summary>
/// Читатель строк
/// </summary>
public interface IRowReader : IAsyncDisposable
{
    /// <summary>
    /// Читает следующую строку
    /// </summary>
    ValueTask<IRow?> Read();
}