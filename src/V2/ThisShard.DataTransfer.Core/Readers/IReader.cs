using ThisShard.Database.Models.Rows;

namespace ThisShard.Database.Readers;

/// <summary>
/// Интерфейс ридера
/// </summary>
public interface IReader : IAsyncDisposable
{
    /// <summary>
    /// Читает следующую строку
    /// </summary>
    ValueTask<IRow?> Read();
}