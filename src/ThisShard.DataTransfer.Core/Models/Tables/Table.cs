using ThisShard.Database.Core.Models.Columns;

namespace ThisShard.Database.Core.Models.Tables;

/// <summary>
/// Базовая имплементация таблицы
/// </summary>
public class Table : ITable
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
    /// Исходный путь
    /// </summary>
    public required string[] RawPath { get; set; }
    
    /// <summary>
    /// Столбцы
    /// </summary>
    public required IReadOnlyList<Column> Columns { get; set; }
    
    /// <summary>
    /// Столбцы
    /// </summary>
    IReadOnlyList<IColumn> ITable.Columns => Columns;
}