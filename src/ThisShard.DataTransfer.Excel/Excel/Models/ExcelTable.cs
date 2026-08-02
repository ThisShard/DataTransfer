using ThisShard.Database.Core.Models.Columns;
using ThisShard.Database.Core.Models.Tables;

namespace ThisShard.Database.Infrastructure.Excel.Models;

/// <summary>
/// Таблица Json
/// </summary>
public class ExcelTable : ITable
{
    /// <summary>
    /// Ключ
    /// </summary>
    public required string Key { get; set; }
    
    /// <summary>
    /// Столбцы таблицы
    /// </summary>
    public required IReadOnlyList<ExcelColumn> Columns { get; set; }
    
    /// <summary>
    /// Столбцы таблицы
    /// </summary>
    IReadOnlyList<IColumn> ITable.Columns => Columns;

    /// <summary>
    /// Путь в БД
    /// </summary>
    string ITable.Path => Key;

    /// <summary>
    /// Исходный путь
    /// </summary>
    string[] ITable.RawPath => [Key];
}