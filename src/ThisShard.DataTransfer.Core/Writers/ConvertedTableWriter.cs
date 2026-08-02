using ThisShard.Database.Core.Models;
using ThisShard.Database.Core.Models.Rows;
using ThisShard.Database.Core.Models.Tables;

namespace ThisShard.Database.Core.Writers;

public class ConvertedTableWriter : ITableWriter
{
    private readonly ITableWriter _tableWriter;
    private readonly Func<ITable, Func<IRow, IRow?>> _converterFactory;
    private Func<IRow, IRow?>? _converter;

    /// <summary>
    /// Текущая таблица
    /// </summary>
    public ITable Table => _tableWriter.Table;

    /// <summary>
    /// Текущая временная таблица
    /// </summary>
    public IStagingTable? StagingTable => _tableWriter.StagingTable;

    /// <summary>
    /// Состояние писателя
    /// </summary>
    public WriterState State => _tableWriter.State;

    /// <summary>
    /// Строки ожидающие обработку
    /// </summary>
    public IEnumerable<IRow> PendingRows => _tableWriter.PendingRows;

    public ConvertedTableWriter(ITableWriter tableWriter, Func<ITable, Func<IRow, IRow?>> converterFactory)
    {
        _tableWriter = tableWriter ?? throw new ArgumentNullException(nameof(tableWriter));
        _converterFactory = converterFactory ?? throw new ArgumentNullException(nameof(converterFactory));
    }

    /// <summary>
    /// Инициализация таблицей
    /// </summary>
    public async ValueTask Init(ITable table)
    {
        await _tableWriter.Init(table);
    }

    /// <summary>
    /// Инициализация временной таблицей
    /// </summary>
    public async ValueTask Init(IStagingTable stagingTable)
    {
        await _tableWriter.Init(stagingTable);
    }

    /// <summary>
    /// Записывает множество строк
    /// </summary>
    public async ValueTask Write(IEnumerable<IRow> rows)
    {
        _converter ??= _converterFactory(Table);
        await _tableWriter.Write(rows.Select(_converter).Where(x=>x != null)!);
    }
    
    /// <summary>
    /// Записывает строку
    /// </summary>
    public async ValueTask Write(IRow row)
    {
        _converter ??= _converterFactory(Table);
        
        var convertedRow = _converter(row);
        if (convertedRow == null)
            return;
        
        await _tableWriter.Write(convertedRow);
    }

    public async ValueTask Flush()
    {
        await _tableWriter.Flush();
    }
    
    /// <summary>
    /// Завершает запись
    /// </summary>
    public async ValueTask Complete()
    {
        await _tableWriter.Complete();
    }

    /// <summary>Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources asynchronously.</summary>
    /// <returns>A task that represents the asynchronous dispose operation.</returns>
    public async ValueTask DisposeAsync()
    {
        await _tableWriter.DisposeAsync();
    }
}