using ThisShard.Database.Core.Models;
using ThisShard.Database.Core.Models.Columns;

namespace ThisShard.Database.Infrastructure.Postgres.Models;

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