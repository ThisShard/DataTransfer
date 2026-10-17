namespace ThisShard.Database.Models.Columns;

/// <summary>
/// Схема колонки во временной таблице
/// </summary>
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