using ThisShard.Database.Core.Models;
using ThisShard.Database.Core.Models.Columns;

namespace ThisShard.Database.Core.Schemas;

public interface IStagingColumnSchema
{
    /// <summary>
    /// Колонка в таблице назначения
    /// </summary>
    IColumn DestinationColumn { get; init; }

    /// <summary>
    /// Колонка со значением
    /// </summary>
    IStagingColumn ValueColumn { get; init; }

    /// <summary>
    /// Колонка с флагом наличия значения
    /// </summary>
    IStagingColumn FlagColumn { get; init; }
}