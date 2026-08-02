using ThisShard.Database.Core.Models;
using ThisShard.Database.Core.Models.Columns;
using ThisShard.Database.Core.Models.Tables;

namespace ThisShard.Database.Core.Schemas;

public interface ITableSchema
{
    /// <summary>
    /// Таблица
    /// </summary>
    ITable Table { get; }

    /// <summary>
    /// Мэппинги колонок к именам параметров 
    /// </summary>
    IReadOnlyDictionary<IColumn, string> ColumnParameterMappings { get; }

    /// <summary>
    /// Изменяемые колонки
    /// </summary>
    IReadOnlyList<IColumn> MutableColumns { get; }

    /// <summary>
    /// Колонки первичного ключа
    /// </summary>
    IReadOnlyList<IColumn> PrimaryKeyColumns { get; }

    /// <summary>
    /// Остальные колонки
    /// </summary>
    IReadOnlyList<IColumn> NonPrimaryKeyColumns { get; }

    /// <summary>
    /// Есть возможность вставки
    /// </summary>
    bool CanInsert { get; }

    /// <summary>
    /// Есть возможность обновления
    /// </summary>
    bool CanUpdate { get; }

    /// <summary>
    /// Есть возможность удаления
    /// </summary>
    bool CanDelete { get; }
}