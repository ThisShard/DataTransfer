using System.Threading.Channels;
using ThisShard.Database.Core.Models;
using ThisShard.Database.Core.Models.Rows;
using ThisShard.Database.Core.Writers;

namespace ThisShard.Database.Core.Readers;

/// <summary>
/// Читатель через пайп
/// </summary>
public class PipeReader : IRowReader
{
    private readonly Channel<IRow> _channel = Channel.CreateBounded<IRow>(new BoundedChannelOptions(1)
    {
        FullMode = BoundedChannelFullMode.Wait
    });
    private readonly CancellationTokenSource _cts = new();
    
    private readonly Func<IRowWriter, CancellationToken, Task> _writeFunc;
    
    private Task? _reading;

    public PipeReader(Func<IRowWriter, CancellationToken, Task> reading)
    {
        _writeFunc = reading ?? throw new ArgumentNullException(nameof(reading));
    }

    public async ValueTask DisposeAsync()
    {
        await _cts.CancelAsync();
        _channel.Writer.TryComplete(new ObjectDisposedException("PipeReader is disposed"));
    }

    /// <summary>
    /// Читает следующую строку
    /// </summary>
    public async ValueTask<IRow?> Read()
    {
        if (_cts.IsCancellationRequested)
            throw new ObjectDisposedException("PipeReader is disposed");

        Startreading();
        
        if (!await _channel.Reader.WaitToReadAsync())
            return null;
        
        _channel.Reader.TryRead(out var row);
        return row;
    }

    /// <summary>
    /// Начало записи
    /// </summary>
    private void Startreading()
    {
        if (_reading != null)
            return;

        _reading = reading();
    }
    
    /// <summary>
    /// Выполнение действия записи
    /// </summary>
    private async Task reading()
    {
        try
        {
            var writer = new Writer(_channel);
            await _writeFunc(writer, _cts.Token);
            _channel.Writer.Complete();
        }
        catch(Exception ex)
        {
            _channel.Writer.TryComplete(ex);
        }
    }
    
    /// <summary>
    /// Писатель в пайп
    /// </summary>
    private class Writer : IRowWriter
    {
        private readonly Channel<IRow> _channel;

        /// <summary>
        /// Строки ожидающие обработку
        /// </summary>
        public IEnumerable<IRow> PendingRows => [];

        public Writer(Channel<IRow> channel)
        {
            _channel = channel ?? throw new ArgumentNullException(nameof(channel));
        }

        /// <summary>
        /// Записывает множество строк
        /// </summary>
        public async ValueTask Write(IEnumerable<IRow> rows)
        {
            foreach (var row in rows)
            {
                await Write(row);
            }
        }

        /// <summary>
        /// Записывает строку
        /// </summary>
        public async ValueTask Write(IRow row) => await _channel.Writer.WriteAsync(row);

        /// <summary>
        /// Принудительно производит запись
        /// </summary>
        public ValueTask Flush() => ValueTask.CompletedTask;
    }
}