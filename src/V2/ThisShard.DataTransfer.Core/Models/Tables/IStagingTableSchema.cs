using ThisShard.Database.Models.Columns;

namespace ThisShard.Database.Models.Tables;

public interface IStagingTableSchema
{
    /// <summary>
    /// Временная таблица
    /// </summary>
    IStagingTable Table { get; init; }

    /// <summary>
    /// Колонка с Id батча
    /// </summary>
    IStagingColumn BatchIdColumn { get; init; }

    /// <summary>
    /// Колонка состояния строки
    /// </summary>
    IStagingColumn RowStateColumn { get; init; }

    /// <summary>
    /// Столбцы во временной таблице пригодные для записи
    /// </summary>
    IReadOnlyList<IStagingColumn> StagingColumns { get; init; }

    /// <summary>
    /// Столбцы во временной таблице в соответствии с таблицей назначения
    /// </summary>
    IReadOnlyList<IStagingColumnSchema> Columns { get; init; }

    /// <summary>
    /// Мутабельные столбцы во временной таблице
    /// </summary>
    IReadOnlyList<IStagingColumnSchema> MutableColumns { get; init; }

    /// <summary>
    /// Столбцы первичного ключа во временной таблице
    /// </summary>
    IReadOnlyList<IStagingColumnSchema> PrimaryKeyColumns { get; init; }

    /// <summary>
    /// Остальные столбцы во временной таблице
    /// </summary>
    IReadOnlyList<IStagingColumnSchema> NonPrimaryKeyColumns { get; init; }
}