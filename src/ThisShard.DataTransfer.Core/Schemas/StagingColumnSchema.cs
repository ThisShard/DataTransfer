using ThisShard.Database.Core.Models;
using ThisShard.Database.Core.Models.Columns;

namespace ThisShard.Database.Core.Schemas;

public class StagingColumnSchema : IStagingColumnSchema
{
    /// <summary>
    /// Колонка в таблице назначения
    /// </summary>
    public required IColumn DestinationColumn { get; init; }
        
    /// <summary>
    /// Колонка со значением
    /// </summary>
    public required IStagingColumn ValueColumn { get; init; }
        
    /// <summary>
    /// Колонка с флагом наличия значения
    /// </summary>
    public required IStagingColumn FlagColumn { get; init; }
}