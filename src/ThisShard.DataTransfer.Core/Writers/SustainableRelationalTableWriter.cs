using System.Data.Common;
using ThisShard.Database.Core.Extensions;
using ThisShard.Database.Core.Models;
using ThisShard.Database.Core.Models.Rows;
using ThisShard.Database.Core.Models.Tables;

namespace ThisShard.Database.Core.Writers;

/// <summary>
/// Устойчивый писатель данных в БД
/// </summary>
public class SustainableRelationalTableWriter<TConnection> : BaseTableWriter
    where TConnection : DbConnection
{
    private readonly TConnection _connection;
    private readonly Func<TConnection, int, ValueTask<ITableWriter?>> _writerFactory;
    private readonly Func<TConnection, Exception, bool> _terminatePredicate;
    private readonly Func<IRow, IRow?>? _rowConverterOnRetry;
    private readonly bool _ownsConnection;
    private readonly bool _shouldCloseConnection;

    private IRow[] _pendingRows = Array.Empty<IRow>();
    private ITableWriter? _writer;

    private bool _isInitializedByTable;
    private bool _isInitializedByStagingTable;

    /// <summary>
    /// Строки ожидающие обработку
    /// </summary>
    public override IEnumerable<IRow> PendingRows => _writer?.PendingRows ?? _pendingRows;

    public SustainableRelationalTableWriter(
        TConnection connection, 
        Func<TConnection, int, ValueTask<ITableWriter?>> writerFactory, 
        Func<TConnection, Exception, bool> terminatePredicate, 
        bool isInnerWriterInitialized, 
        Func<IRow, IRow?>? rowConverterOnRetry = null, 
        bool ownsConnection = false)
    {
        _connection = connection ?? throw new ArgumentNullException(nameof(connection));
        _writerFactory = writerFactory ?? throw new ArgumentNullException(nameof(writerFactory));
        _terminatePredicate = terminatePredicate ?? throw new ArgumentNullException(nameof(terminatePredicate));
        _rowConverterOnRetry = rowConverterOnRetry;
        _ownsConnection = ownsConnection;
        
        _shouldCloseConnection = !_connection.IsOpen();

        if (isInnerWriterInitialized)
            State = WriterState.Initialized;
    }

    /// <summary>
    /// Действие при инициализации таблицей
    /// </summary>
    protected override ValueTask OnInit(ITable table) => DoWithWriter(async wr =>
    {
        await wr.Init(table);
    });

    /// <summary>
    /// Дейстивие после успешной инициализации
    /// </summary>
    protected override async ValueTask OnInitCompleted(ITable table)
    {
        await base.OnInitCompleted(table);
        _isInitializedByTable = true;
    }

    /// <summary>
    /// Действие при инициализации временной таблицей
    /// </summary>
    protected override ValueTask OnInit(IStagingTable table) => DoWithWriter(async wr =>
    {
        await wr.Init(table);
    });

    /// <summary>
    /// Дейстивие после успешной инициализации
    /// </summary>
    protected override async ValueTask OnInitCompleted(IStagingTable table)
    {
        await base.OnInitCompleted(table);
        _isInitializedByStagingTable = true;
    }

    /// <summary>
    /// Действие при записи строк
    /// </summary>
    protected override ValueTask OnWrite(IEnumerable<IRow> rows) => DoWithWriter(async wr =>
    {
        await wr.Write(rows);
    });

    /// <summary>
    /// Действие при записи одиночной строки
    /// </summary>
    protected override ValueTask OnWrite(IRow row) => DoWithWriter(async wr =>
    {
        await wr.Write(row);
    });

    /// <summary>
    /// Действие при принудительной записи
    /// </summary>
    protected override ValueTask OnFlush() => DoWithWriter(async wr =>
    {
        await wr.Flush();
    });

    /// <summary>
    /// Действие при завершении записи
    /// </summary>
    protected override ValueTask OnComplete() => DoWithWriter(async wr =>
    {
        await wr.Complete();
    });

    protected override async ValueTask OnDispose()
    {
        await DisposeWriter();

        if (_shouldCloseConnection)
            await _connection.CloseAsync();

        if (_ownsConnection)
            await _connection.DisposeAsync();
    }

    /// <summary>
    /// Выполняет действие с писателем
    /// </summary>
    private async ValueTask DoWithWriter(Func<ITableWriter, ValueTask> action)
    {
        while (true)
        {
            var writer = await GetWriter();
            try
            {
                await action(writer);
                return;
            }
            catch (Exception ex)
            {
                await DisposeWriter();
                
                if (_terminatePredicate(_connection, ex))
                    throw;

            }
        }
    }

    /// <summary>
    /// Диспозит писателя
    /// </summary>
    private async ValueTask DisposeWriter()
    {
        if (_writer == null)
            return;
        
        _pendingRows = _writer.PendingRows.ToArray();
        await _writer.DisposeAsync();
        _writer = null;
    }

    /// <summary>
    /// Возвращает писателя
    /// </summary>
    private async ValueTask<ITableWriter> GetWriter()
    {
        if (_writer != null)
            return _writer;

        var writer = await CreateWriter();
        _writer = writer ?? throw new InvalidOperationException("Could not create writer");
        return _writer;
    }

    /// <summary>
    /// Создает писателя из фабрики
    /// </summary>
    private async ValueTask<ITableWriter?> CreateWriter()
    {
        var retryCount = 0;
        while (true)
        {
            try
            {
                if (!_connection.IsOpen())
                    await _connection.OpenAsync();
                
                var writer = await _writerFactory(_connection, retryCount);
                if (writer == null)
                    return null;

                await RestoreWriterState(writer);

                return writer;
            }
            catch (Exception ex)
            {
                if (_terminatePredicate(_connection, ex))
                    throw;
                
                retryCount++;
            }
        }
    }

    /// <summary>
    /// Восстановление текущего состояния на новом писателе
    /// </summary>
    private async ValueTask RestoreWriterState(ITableWriter writer)
    {
        //Инициализация временной таблицей
        if (_isInitializedByStagingTable)
            await writer.Init(StagingTable!);

        //Инициализация таблицей
        if (_isInitializedByTable)
            await writer.Init(Table);
                
        //Запись незавершенных строк
        if (_pendingRows.Length > 0)
        {
            if (_rowConverterOnRetry != null)
                await writer.UsingConverter(_rowConverterOnRetry).Write(_pendingRows);
            else
                await writer.Write(_pendingRows);
        }

        //Очищение буфера незавершенных строк
        _pendingRows = Array.Empty<IRow>();
    }
}