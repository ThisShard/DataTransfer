namespace ThisShard.Database.Core.Models.Columns;

/// <summary>
/// Базовая имплементация столбца
/// </summary>
public class Column : IColumn
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
    /// Тип
    /// </summary>
    public required Type Type { get; set; }

    /// <summary>
    /// Только для чтения
    /// </summary>
    public bool IsReadOnly { get; set; }

    /// <summary>
    /// Является первичным ключом
    /// </summary>
    public bool IsPrimaryKey { get; set; }

    /// <summary>
    /// Является нуллабельным
    /// </summary>
    public bool IsNullable { get; set; } = true;
}