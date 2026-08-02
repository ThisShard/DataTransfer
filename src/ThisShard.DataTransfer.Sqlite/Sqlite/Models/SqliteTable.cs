using ThisShard.Database.Core.Models;
using ThisShard.Database.Core.Models.Columns;
using ThisShard.Database.Core.Models.Tables;

namespace ThisShard.Database.Infrastructure.Sqlite.Models;

/// <summary>
/// Таблица Sqlite
/// </summary>
public class SqliteTable : ITable
{
    /// <summary>
    /// Ключ
    /// </summary>
    public required string Key { get; set; }

    /// <summary>
    /// Путь в БД
    /// </summary>
    public required string Path { get; set; }

    /// <summary>
    /// Исходное имя
    /// </summary>
    public required string RawName { get; set; }
    
    /// <summary>
    /// Столбцы таблицы
    /// </summary>
    public required IReadOnlyList<SqliteColumn> Columns { get; set; }
    
    /// <summary>
    /// Столбцы таблицы
    /// </summary>
    IReadOnlyList<IColumn> ITable.Columns => Columns;

    /// <summary>
    /// Исходный путь
    /// </summary>
    string[] ITable.RawPath => [RawName];
}