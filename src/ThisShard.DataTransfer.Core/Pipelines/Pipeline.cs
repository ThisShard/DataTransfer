using ThisShard.Database.Core.Extensions;
using ThisShard.Database.Core.Helpers;
using ThisShard.Database.Core.Models.Results;
using ThisShard.Database.Core.Writers;

namespace ThisShard.Database.Core.Pipelines;

/// <summary>
/// Конвейер
/// </summary>
public class Pipeline : IPipeline<WritingResult>
{
    private readonly TaskCompletionSource _taskCompletionSource = new();
    private readonly CancellationTokenSource _cancellationTokenSource = new();
    
    private readonly IPipelineSource[] _sources;
    private readonly IPipelineDestination[] _destinations;
    
    private bool _isStarted;

    /// <summary>
    /// Задача завершения конвейера
    /// </summary>
    public Task Completion => _taskCompletionSource.Task;

    /// <summary>
    /// Результат завершения задачи конвейера
    /// </summary>
    public WritingResult? Result { get; private set; }

    public Pipeline(IEnumerable<IPipelineSource> sources, IEnumerable<IPipelineDestination> destinations)
    {
        _sources = sources?.ToArray() ?? throw new ArgumentNullException(nameof(sources));
        _destinations = destinations?.ToArray() ?? throw new ArgumentNullException(nameof(destinations));
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
                Result = task.Result;
            }
        });

        return ValueTask.CompletedTask;
    }

    /// <summary>
    /// Работа
    /// </summary>
    private async Task<WritingResult> Work()
    {
        if (_cancellationTokenSource.IsCancellationRequested)
            return new WritingResult { State = WritingState.Canceled };
        
        var writers = await GetWriters();
        try
        {
            var lastResult = new WritingResult
            {
                State = WritingState.Success,
                Writers = writers
            };
            
            if (_cancellationTokenSource.IsCancellationRequested)
                return lastResult with { State = WritingState.Canceled };

            foreach (var source in _sources)
            {
                if (_cancellationTokenSource.IsCancellationRequested)
                    return lastResult with { State = WritingState.Canceled };
                
                await using var reader = await source.GetReader();
                lastResult = await reader.TryWriteTo(writers, _cancellationTokenSource.Token);
                if (lastResult.State != WritingState.Success)
                    break;
            }

            if (lastResult.State != WritingState.Error)
                await CompleteWriters(writers);
            
            return lastResult;
        }
        finally
        {
            await DisposeHelper.DisposeMany(writers);
        }
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