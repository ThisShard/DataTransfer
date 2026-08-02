using ThisShard.Database.Core.Models;
using ThisShard.Database.Core.Models.Rows;
using ThisShard.Database.Core.Models.Tables;

namespace ThisShard.Database.Core.Writers;

/// <summary>
/// Базовый писатель данных в таблцу
/// </summary>
public abstract class BaseTableWriter : BaseRowWriter, ITableWriter
{
    /// <summary>
    /// Текущая таблица
    /// </summary>
    public ITable Table { get; protected set; }

    /// <summary>
    /// Текущая временная таблица
    /// </summary>
    public IStagingTable? StagingTable { get; protected set; }

    protected BaseTableWriter()
    {
        State = WriterState.Created;
    }
    
    /// <summary>
    /// Инициализация таблицей
    /// </summary>
    public async ValueTask Init(ITable table)
    {
        if (State != WriterState.Created)
            throw new InvalidOperationException();
        
        await OnInit(table);
        await OnInitCompleted(table);
        
        State = WriterState.Initialized;
    }
    
    /// <summary>
    /// Инициализация временной таблицей
    /// </summary>
    public async ValueTask Init(IStagingTable stagingTable)
    {
        if (State != WriterState.Created)
            throw new InvalidOperationException();
        
        await OnInit(stagingTable);
        await OnInitCompleted(stagingTable);
        
        State = WriterState.Initialized;
    }

    #region Abstract

    /// <summary>
    /// Действие при инициализации таблицей
    /// </summary>
    protected abstract ValueTask OnInit(ITable table);
    
    /// <summary>
    /// Действие при инициализации временной таблицей
    /// </summary>
    protected abstract ValueTask OnInit(IStagingTable table);

    /// <summary>
    /// Дейстивие после успешной инициализации
    /// </summary>
    protected virtual ValueTask OnInitCompleted(ITable table)
    {
        Table = table;
        return ValueTask.CompletedTask;
    }

    /// <summary>
    /// Дейстивие после успешной инициализации
    /// </summary>
    protected virtual ValueTask OnInitCompleted(IStagingTable table)
    {
        StagingTable = table;
        Table = table.DestinationTable;
        return ValueTask.CompletedTask;
    }
    
    #endregion

}