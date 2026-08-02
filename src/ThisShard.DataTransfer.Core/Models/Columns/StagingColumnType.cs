namespace ThisShard.Database.Core.Models.Columns;

public enum StagingColumnType
{
    /// <summary>
    /// Игнорировать
    /// </summary>
    Ignored = 0,
    
    /// <summary>
    /// Id батча
    /// </summary>
    BatchId = 1,
    
    /// <summary>
    /// Состояние строки
    /// </summary>
    RowState = 2,
    
    /// <summary>
    /// Данные
    /// </summary>
    Data = 3,
    
    /// <summary>
    /// Флаг изменения данных
    /// </summary>
    DataModificationFlag = 4
}