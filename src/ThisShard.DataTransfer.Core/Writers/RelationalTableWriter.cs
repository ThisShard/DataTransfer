using System.Data;
using ThisShard.Database.Core.Models;
using ThisShard.Database.Core.Models.Columns;
using ThisShard.Database.Core.Models.Rows;
using ThisShard.Database.Core.Schemas;

namespace ThisShard.Database.Core.Writers;

public abstract class RelationalTableWriter : BufferedTableWriter
{
    /// <summary>
    /// Схема таблицы
    /// </summary>
    protected ITableSchema TableSchema { get; set; } = null!;

    // ReSharper disable once ConvertToPrimaryConstructor
    protected RelationalTableWriter(int bufferSize) : base(bufferSize)
    {
    }

    #region Фильтрация строк

    /// <summary>
    /// Проверка на то что нужно ли добавлять строку в буфер
    /// </summary>
    protected override bool ShouldAddRowToBuffer(IRow row)
    {
        switch (row.State)
        {
            case RowState.Added:
                return TableSchema.CanInsert;
            case RowState.AddedOrModified when TableSchema.CanInsert:
                return true;
            case RowState.AddedOrModified:
            case RowState.Modified:
                return TableSchema.CanUpdate && HasModifications(row);
            case RowState.SafeDeleted:
            case RowState.Deleted:
                return TableSchema.CanUpdate;
        }
        
        return false;
    }

    /// <summary>
    /// Проверяет наличие изменений в строке
    /// </summary>
    protected virtual bool HasModifications(IRow row)
    {
        var columnsToCheck = row.State == RowState.AddedOrModified 
            ? TableSchema.MutableColumns 
            : TableSchema.NonPrimaryKeyColumns;

        return GetModifications(row, columnsToCheck).Any();
    }

    /// <summary>
    /// Возвращает модификации по столбцам
    /// </summary>
    protected virtual IEnumerable<KeyValuePair<IColumn, object?>> GetModifications(IRow row, IEnumerable<IColumn> columns)
    {
        foreach (var column in columns)
        {
            if (row.TryGetValue(column.Key, out var value))
                yield return new KeyValuePair<IColumn, object?>(column, value);
        }
    }

    #endregion
    
    #region Валидация

    /// <summary>
    /// Валидация после исполнения
    /// </summary>
    protected virtual void ValidateCount(int count)
    {
        var expectedCount = Buffer.Count(x => ShouldValidateCount(x.State));
        if (count < expectedCount)
            throw new DBConcurrencyException();
    }

    /// <summary>
    /// Должен валидировать количество
    /// </summary>
    protected virtual bool ShouldValidateCount(RowState state)
    {
        return state is RowState.Added or RowState.Deleted or RowState.Modified or RowState.AddedOrModified;
    }

    #endregion
}