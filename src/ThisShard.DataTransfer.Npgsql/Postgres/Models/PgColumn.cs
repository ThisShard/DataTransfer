using ThisShard.Database.Core.Models;
using ThisShard.Database.Core.Models.Columns;

namespace ThisShard.Database.Infrastructure.Postgres.Models;

/// <summary>
/// Столбец Postgres
/// </summary>
public class PgColumn : IColumn
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
    /// Тип данных в БД
    /// </summary>
    public required string DataTypeName { get; set; }

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

    /// <summary>
    /// Порядковый номер колонки в первичном ключе
    /// </summary>
    public int? PrimaryKeyOrdinal { get; set; }
    
    /// <summary>
    /// Сортировка по убыванию в первичном ключе
    /// </summary>
    public bool? PrimaryKeyDesc { get; set; }
    
    /// <summary>
    /// Нуллы в первичном ключе идут в начале
    /// </summary>
    public bool? PrimaryKeyNullsFirst { get; set; }
}