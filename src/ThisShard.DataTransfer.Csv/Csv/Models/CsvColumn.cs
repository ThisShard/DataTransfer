using ThisShard.Database.Core.Models.Columns;

namespace ThisShard.Database.Infrastructure.Csv.Models;

/// <summary>
/// Колонка в Csv
/// </summary>
public class CsvColumn : IColumn
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
    Type IColumn.Type => typeof(string);

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