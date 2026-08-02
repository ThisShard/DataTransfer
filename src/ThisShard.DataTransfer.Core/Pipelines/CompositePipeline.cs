using ThisShard.Database.Core.Helpers;
using ThisShard.Database.Core.Models.Results;

namespace ThisShard.Database.Core.Pipelines;

/// <summary>
/// Составной конвейер
/// </summary>
public class CompositePipeline<TPipeline> : ICompositePipeline<TPipeline>
    where TPipeline : IPipeline
{
    private readonly TaskCompletionSource _taskCompletionSource = new();
    private readonly CancellationTokenSource _cancellationTokenSource = new();
    
    private readonly TPipeline[] _pipelines;
    private readonly int _maxDop;
    
    private bool _isStarted;

    /// <summary>
    /// Задача завершения конвейера
    /// </summary>
    public Task Completion => _taskCompletionSource.Task;

    /// <summary>
    /// Пайплайны
    /// </summary>
    public IEnumerable<TPipeline> Pipelines => _pipelines;

    public CompositePipeline(IEnumerable<TPipeline> pipelines, int maxDop = 1)
    {
        _pipelines = pipelines?.ToArray() ?? throw new ArgumentNullException(nameof(pipelines));
        _maxDop = maxDop;
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
                _taskCompletionSource.TrySetResult();
        });

        return ValueTask.CompletedTask;
    }

    /// <summary>
    /// Работа
    /// </summary>
    private async Task Work()
    {
        if (_cancellationTokenSource.IsCancellationRequested)
            return;

        await Parallel.ForEachAsync(_pipelines, new ParallelOptions()
        {
            CancellationToken =  _cancellationTokenSource.Token,
            MaxDegreeOfParallelism = _maxDop
        }, async (pipeline, ct) =>
        {
            if (ct.IsCancellationRequested)
                return;
            
            await pipeline.Start();
            await pipeline.Completion;
        });
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
        await DisposeHelper.DisposeMany(_pipelines.Cast<IAsyncDisposable>());
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