using System.Threading.Channels;
using ThisShard.Database.Models.Rows;
using ThisShard.Database.Writers;
using ThisShard.Database.Writers.States;

namespace ThisShard.Database.Readers;

/// <summary>
/// Ридер через пайп
/// </summary>
public class PipeReader : IReader
{
    private readonly Channel<IRow> _channel = Channel.CreateBounded<IRow>(new BoundedChannelOptions(1)
    {
        FullMode = BoundedChannelFullMode.Wait
    });
    private readonly CancellationTokenSource _cts = new();
    
    private readonly Func<IWriter, CancellationToken, Task> _writeFunc;
    
    private Task? _reading;

    public PipeReader(Func<IWriter, CancellationToken, Task> reading)
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

        StartReading();
        
        if (!await _channel.Reader.WaitToReadAsync())
            return null;
        
        _channel.Reader.TryRead(out var row);
        return row;
    }

    /// <summary>
    /// Начало записи
    /// </summary>
    private void StartReading()
    {
        if (_reading != null)
            return;

        _reading = Reading();
    }
    
    /// <summary>
    /// Выполнение действия записи
    /// </summary>
    private async Task Reading()
    {
        try
        {
            var writer = new PipeWriter(_channel);
            await _writeFunc(writer, _cts.Token);
            _channel.Writer.Complete();
        }
        catch(Exception ex)
        {
            _channel.Writer.TryComplete(ex);
        }
    }
    
    /// <summary>
    /// Райтер в пайп
    /// </summary>
    private class PipeWriter : IWriter
    {
        private readonly Channel<IRow> _channel;

        /// <summary>
        /// Состояние писателя
        /// </summary>
        public WriterStatus Status => WriterStatus.Writing;

        /// <summary>
        /// Размер буфера
        /// </summary>
        public int BufferSize => 1;

        /// <summary>
        /// Вернуть буфер строк без извлечения
        /// </summary>
        public IEnumerable<IRow> PeekBuffer() => Enumerable.Empty<IRow>();

        /// <summary>
        /// Вернуть буфер строк с извлечением
        /// </summary>
        public IReadOnlyList<IRow> PopBuffer() => Array.Empty<IRow>();

        /// <summary>
        /// Производит инициализацию
        /// </summary>
        public ValueTask Init() => ValueTask.CompletedTask;

        public PipeWriter(Channel<IRow> channel)
        {
            _channel = channel ?? throw new ArgumentNullException(nameof(channel));
        }

        /// <summary>
        /// Записывает строку
        /// </summary>
        public async ValueTask Write(IRow row) => await _channel.Writer.WriteAsync(row);

        /// <summary>
        /// Принудительно производит запись
        /// </summary>
        public ValueTask Flush() => ValueTask.CompletedTask;

        /// <summary>
        /// Завершает запись
        /// </summary>
        public ValueTask Complete() => ValueTask.CompletedTask;

        /// <summary>Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources asynchronously.</summary>
        /// <returns>A task that represents the asynchronous dispose operation.</returns>
        public ValueTask DisposeAsync() => ValueTask.CompletedTask;
    }
}