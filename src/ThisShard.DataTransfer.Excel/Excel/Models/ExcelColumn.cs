using ThisShard.Database.Core.Models.Columns;

namespace ThisShard.Database.Infrastructure.Excel.Models;

/// <summary>
/// Столбец Json
/// </summary>
public class ExcelColumn : IColumn
{
    /// <summary>
    /// Ключ
    /// </summary>
    public required string Key { get; set; }

    /// <summary>
    /// Имя свойства
    /// </summary>
    public required string Name { get; set; }

    /// <summary>
    /// Тип
    /// </summary>
    public required Type Type { get; set; }

    /// <summary>
    /// Путь в БД
    /// </summary>
    string IColumn.Path => Name;

    /// <summary>
    /// Исходное имя
    /// </summary>
    string IColumn.RawName => Name;
    
    /// <summary>
    /// Только для чтения
    /// </summary>
    bool IColumn.IsReadOnly => false;

    /// <summary>
    /// Является первичным ключом
    /// </summary>
    bool IColumn.IsPrimaryKey => false;

    /// <summary>
    /// Является нуллабельным
    /// </summary>
    bool IColumn.IsNullable => true;
}