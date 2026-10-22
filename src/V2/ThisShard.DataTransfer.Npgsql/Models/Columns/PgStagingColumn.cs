namespace ThisShard.Database.Models.Columns;

/// <summary>
/// Столбец временной таблицы Postgres
/// </summary>
public class PgStagingColumn : PgColumn, IStagingColumn
{
    /// <summary>
    /// Связанный столбец в основной таблице
    /// </summary>
    public PgColumn? LinkedColumn { get; set; }

    /// <summary>
    /// Тип столбца временной таблицы
    /// </summary>
    public StagingColumnType StagingColumnType { get; set; }

    /// <summary>
    /// Связанный столбец в основной таблице
    /// </summary>
    IColumn? IStagingColumn.LinkedColumn => LinkedColumn;
}