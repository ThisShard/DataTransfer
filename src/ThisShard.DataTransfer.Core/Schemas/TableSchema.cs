using ThisShard.Database.Core.Models;
using ThisShard.Database.Core.Models.Columns;
using ThisShard.Database.Core.Models.Tables;

namespace ThisShard.Database.Core.Schemas;

public class TableSchema : ITableSchema
{
    /// <summary>
    /// Таблица
    /// </summary>
    public required ITable Table { get; init; }
    
    /// <summary>
    /// Мэппинги колонок к именам параметров 
    /// </summary>
    public required IReadOnlyDictionary<IColumn, string> ColumnParameterMappings { get; init; }
    
    /// <summary>
    /// Изменяемые колонки
    /// </summary>
    public required IReadOnlyList<IColumn> MutableColumns { get; init; }
    
    /// <summary>
    /// Колонки первичного ключа
    /// </summary>
    public required IReadOnlyList<IColumn> PrimaryKeyColumns { get; init; }
    
    /// <summary>
    /// Остальные колонки
    /// </summary>
    public required IReadOnlyList<IColumn> NonPrimaryKeyColumns { get; init; }
    
    /// <summary>
    /// Есть возможность вставки
    /// </summary>
    public bool CanInsert { get; init; }
    
    /// <summary>
    /// Есть возможность обновления
    /// </summary>
    public bool CanUpdate { get; init; }
    
    /// <summary>
    /// Есть возможность удаления
    /// </summary>
    public bool CanDelete { get; init; }
}