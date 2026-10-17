using ThisShard.Database.Models.Columns;

namespace ThisShard.Database.Models.Tables;

/// <summary>
/// Интерфейс таблицы
/// </summary>
public interface ITable
{
    /// <summary>
    /// Ключ
    /// </summary>
    string Key { get; }
    
    /// <summary>
    /// Путь в БД
    /// </summary>
    string Path { get; }
    
    /// <summary>
    /// Исходный путь
    /// </summary>
    string[] RawPath { get; }
    
    /// <summary>
    /// Столбцы
    /// </summary>
    IReadOnlyList<IColumn> Columns { get; }
}