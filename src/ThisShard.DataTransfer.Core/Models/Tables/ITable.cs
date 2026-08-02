using ThisShard.Database.Core.Models.Columns;

namespace ThisShard.Database.Core.Models.Tables;

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