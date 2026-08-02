using ThisShard.Database.Core.Models;
using ThisShard.Database.Core.Models.Rows;

namespace ThisShard.Database.Core.Writers;

/// <summary>
/// Писатель строк
/// </summary>
public interface IRowWriter
{
    /// <summary>
    /// Строки ожидающие обработку
    /// </summary>
    IEnumerable<IRow> PendingRows { get; }
    
    /// <summary>
    /// Записывает множество строк
    /// </summary>
    ValueTask Write(IEnumerable<IRow> rows);

    /// <summary>
    /// Записывает строку
    /// </summary>
    ValueTask Write(IRow row);
    
    /// <summary>
    /// Принудительно производит запись
    /// </summary>
    ValueTask Flush();
}