namespace ThisShard.Database.Core.Models.Columns;

/// <summary>
/// Столбец временной таблицы
/// </summary>
public interface IStagingColumn : IColumn
{
    /// <summary>
    /// Связанный столбец в основной таблице
    /// </summary>
    IColumn? LinkedColumn { get; }
    
    /// <summary>
    /// Тип столбца временной таблицы
    /// </summary>
    StagingColumnType StagingColumnType { get; }
}