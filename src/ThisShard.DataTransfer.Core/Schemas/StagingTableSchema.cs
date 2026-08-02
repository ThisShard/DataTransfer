using ThisShard.Database.Core.Models;
using ThisShard.Database.Core.Models.Columns;
using ThisShard.Database.Core.Models.Tables;

namespace ThisShard.Database.Core.Schemas;

public class StagingTableSchema : IStagingTableSchema
{
    /// <summary>
    /// Временная таблица
    /// </summary>
    public required IStagingTable Table { get; init; }
    
    /// <summary>
    /// Колонка с Id батча
    /// </summary>
    public required IStagingColumn BatchIdColumn { get; init; }
    
    /// <summary>
    /// Колонка состояния строки
    /// </summary>
    public required IStagingColumn RowStateColumn { get; init; }
    
    /// <summary>
    /// Столбцы во временной таблице пригодные для записи
    /// </summary>
    public required IReadOnlyList<IStagingColumn> StagingColumns { get; init; }
    
    /// <summary>
    /// Столбцы во временной таблице в соответствии с таблицей назначения
    /// </summary>
    public required IReadOnlyList<IStagingColumnSchema> Columns { get; init; }
    
    /// <summary>
    /// Мутабельные столбцы во временной таблице
    /// </summary>
    public required IReadOnlyList<IStagingColumnSchema> MutableColumns { get; init; }
    
    /// <summary>
    /// Столбцы первичного ключа во временной таблице
    /// </summary>
    public required IReadOnlyList<IStagingColumnSchema> PrimaryKeyColumns { get; init; }
    
    /// <summary>
    /// Остальные столбцы во временной таблице
    /// </summary>
    public required IReadOnlyList<IStagingColumnSchema> NonPrimaryKeyColumns { get; init; }
}