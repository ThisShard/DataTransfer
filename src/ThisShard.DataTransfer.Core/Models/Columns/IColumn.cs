namespace ThisShard.Database.Core.Models.Columns;

/// <summary>
/// Интерфейс столбца
/// </summary>
public interface IColumn
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
    /// Исходное имя
    /// </summary>
    string RawName { get; }
    
    /// <summary>
    /// Тип
    /// </summary>
    Type Type { get; }
    
    /// <summary>
    /// Только для чтения
    /// </summary>
    bool IsReadOnly { get; }
    
    /// <summary>
    /// Является первичным ключом
    /// </summary>
    bool IsPrimaryKey { get; }
    
    /// <summary>
    /// Является нуллабельным
    /// </summary>
    bool IsNullable { get; }
}