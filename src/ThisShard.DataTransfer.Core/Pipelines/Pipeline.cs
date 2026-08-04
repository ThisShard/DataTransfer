using ThisShard.Database.Core.Extensions;
using ThisShard.Database.Core.Helpers;
using ThisShard.Database.Core.Models.Results;
using ThisShard.Database.Core.Models.States;
using ThisShard.Database.Core.Readers;
using ThisShard.Database.Core.Writers;

namespace ThisShard.Database.Core.Pipelines;

/// <summary>
/// Конвейер
/// </summary>
public class Pipeline : IPipeline<PipelineState>
{
    private readonly TaskCompletionSource _taskCompletionSource = new();
    private readonly CancellationTokenSource _cancellationTokenSource = new();
    
    private readonly IPipelineSource[] _sources;
    private readonly IPipelineDestination[] _destinations;
    
    private PipelineState? _previousState;
    private bool _isStarted;

    /// <summary>
    /// Ключ
    /// </summary>
    public string Key { get; }

    /// <summary>
    /// Задача завершения конвейера
    /// </summary>
    public Task Completion => _taskCompletionSource.Task;
    
    /// <summary>
    /// Результат задачи конвейера
    /// </summary>
    public PipelineState? State { get; private set; }

    public Pipeline(string key, IEnumerable<IPipelineSource> sources, IEnumerable<IPipelineDestination> destinations)
    {
        Key = key;
        _sources = sources?.ToArray() ?? throw new ArgumentNullException(nameof(sources));
        _destinations = destinations?.ToArray() ?? throw new ArgumentNullException(nameof(destinations));
    }

    /// <summary>
    /// Инициализировать предыдущим состоянием перед началом работы
    /// </summary>
    public void Init(PipelineState? state)
    {
        if (_isStarted)
            throw new InvalidOperationException("Pipeline is already started");
        
        _previousState = state;
    }

    /// <summary>
    /// Запускает конвейер
    /// </summary>
    public ValueTask Start()
    {
        if (_isStarted)
            return ValueTask.CompletedTask;

        _isStarted = true;

        Work().ContinueWith(task =>
        {
            if (task.IsFaulted)
                _taskCompletionSource.TrySetException(task.Exception);
            else if (task.IsCanceled)
                _taskCompletionSource.TrySetCanceled();
            else
            {
                _taskCompletionSource.TrySetResult();
                State = task.Result;
            }
        });

        return ValueTask.CompletedTask;
    }

    /// <summary>
    /// Работа
    /// </summary>
    private async Task<PipelineState> Work()
    {
        var currentState = new PipelineState { State = WritingState.Success };
        
        //Процесс
        async Task WorkProcess()
        {
            if (_cancellationTokenSource.IsCancellationRequested)
            {
                currentState = currentState with { State = WritingState.Canceled };
                return;
            }
        
            var writers = await GetWriters();
            try
            {
                if (_cancellationTokenSource.IsCancellationRequested)
                {
                    currentState = currentState with { State = WritingState.Canceled };
                    return;
                }

                foreach (var source in GetSources())
                {
                    if (_cancellationTokenSource.IsCancellationRequested)
                    {
                        currentState = currentState with { State = WritingState.Canceled };
                        break;
                    }

                    await using var reader = await GetReader(source);
                    var writingResult = await reader.TryWriteTo(writers, _cancellationTokenSource.Token);

                    currentState = new PipelineState
                    {
                        LastWrittenSourceKey = source.Key, 
                        State = writingResult.State, 
                        LastWrittenRow = writingResult.LastWrittenRow
                    };

                    if (currentState.State != WritingState.Success)
                        break;
                }

                if (currentState.State != WritingState.Error)
                    await CompleteWriters(writers);
            }
            finally
            {
                await DisposeHelper.DisposeMany(writers);
            }
        }

        try
        {
            await WorkProcess();
            return currentState;
        }
        catch (Exception ex)
        {
            return currentState with
            {
                State = WritingState.Error, 
                Exception = ex
            };
        }
    }

    /// <summary>
    /// Возвращает источники с учетом предыдущего состояния
    /// </summary>
    private IEnumerable<IPipelineSource> GetSources()
    {
        if (_previousState == null || _previousState.LastWrittenSourceKey == null)
            return _sources;

        return _sources.SkipWhile(x => x.Key != _previousState.LastWrittenSourceKey);
    }

    /// <summary>
    /// Возвращает ридер с учетом предыдущего состояния
    /// </summary>
    private async ValueTask<IRowReader> GetReader(IPipelineSource source)
    {
        if (_previousState == null || _previousState.LastWrittenSourceKey != source.Key)
            return await source.GetReader();
        
        return await source.GetReader(_previousState.LastWrittenRow);
    }
    
    /// <summary>
    /// Возвращает писателей
    /// </summary>
    private async ValueTask<List<IRowWriter>> GetWriters()
    {
        var writers = new List<IRowWriter>();
        
        try
        {
            foreach (var destination in _destinations)
            {
                writers.Add(await destination.GetWriter());
            }
        }
        catch (Exception ex)
        {
            await DisposeHelper.DisposeMany(ex, writers);
        }
        
        return writers;
    }

    /// <summary>
    /// Завершает запись у писателей
    /// </summary>
    private async ValueTask CompleteWriters(IEnumerable<IRowWriter> writers)
    {
        foreach (var writer in writers)
        {
            await writer.Complete();
        }
    }

    /// <summary>
    /// Останавливает конвейер
    /// </summary>
    public async ValueTask Stop()
    {
        if (!_isStarted)
            return;
        
        if (_taskCompletionSource.Task.IsCompleted)
            return;

        if (_cancellationTokenSource.IsCancellationRequested)
            return;
        
        await _cancellationTokenSource.CancelAsync();
    }

    /// <summary>Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources asynchronously.</summary>
    /// <returns>A task that represents the asynchronous dispose operation.</returns>
    public async ValueTask DisposeAsync()
    {
        await StopAndAwaitForCompletion();
        await DisposeHelper.DisposeMany(_sources, _destinations);
    }
    
    /// <summary>
    /// Останавливает работу и дожидается ее выполнения
    /// </summary>
    private async ValueTask StopAndAwaitForCompletion()
    {
        if (!_isStarted)
            return;
        
        if (_taskCompletionSource.Task.IsCompleted)
            return;

        await Stop();
        await Completion;
    }
}