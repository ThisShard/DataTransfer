using ThisShard.Database.Core.Helpers;
using ThisShard.Database.Core.Models.Results;
using ThisShard.Database.Core.Models.States;

namespace ThisShard.Database.Core.Pipelines;

/// <summary>
/// Составной конвейер
/// </summary>
public class CompositePipeline : IPipeline<CompositePipelineState>
{
    private readonly TaskCompletionSource _taskCompletionSource = new();
    private readonly CancellationTokenSource _cancellationTokenSource = new();
    
    private readonly IPipeline<PipelineState>[] _pipelines;
    private readonly int _maxDop;
    
    private bool _isStarted;
    private CompositePipelineState? _previousState;

    /// <summary>
    /// Ключ
    /// </summary>
    public string Key { get; }
    
    /// <summary>
    /// Задача завершения конвейера
    /// </summary>
    public Task Completion => _taskCompletionSource.Task;

    /// <summary>
    /// Состояние выполнения задач
    /// </summary>
    public CompositePipelineState? State { get; private set; }
    
    public CompositePipeline(string key, IEnumerable<IPipeline<PipelineState>> pipelines, int maxDop = 1)
    {
        Key = key;
        _pipelines = pipelines?.ToArray() ?? throw new ArgumentNullException(nameof(pipelines));
        _maxDop = maxDop;
    }

    /// <summary>
    /// Инициализировать предыдущим состоянием перед началом работы
    /// </summary>
    public void Init(CompositePipelineState? state)
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
    private async Task<CompositePipelineState> Work()
    {
        if (_cancellationTokenSource.IsCancellationRequested)
            return CreateState(WritingState.Canceled);

        //Процесс в рамках одного пайплайна
        async ValueTask PipelineProcess(IPipeline<PipelineState> pipeline, CancellationToken ct)
        {
            if (ct.IsCancellationRequested)
                return;
            
            var previousState = _previousState?.PipelineStates.GetValueOrDefault(pipeline.Key);
            if (previousState != null)
            {
                if (previousState.State == WritingState.Success)
                    return;
                
                pipeline.Init(previousState);
            }
            
            await pipeline.Start();
            await pipeline.Completion;
        }
        
        //Параллельный процесс
        async Task ParallelWorkProcess()
        {
            await Parallel.ForEachAsync(_pipelines, new ParallelOptions()
            {
                CancellationToken =  _cancellationTokenSource.Token,
                MaxDegreeOfParallelism = _maxDop
            }, PipelineProcess);
        }
        
        //Непараллельный процесс
        async ValueTask WorkProcess()
        {
            foreach (var pipeline in _pipelines)
            {
                await PipelineProcess(pipeline, _cancellationTokenSource.Token);
            }
        }

        try
        {
            if (_maxDop > 1)
                await ParallelWorkProcess();
            else
                await WorkProcess();

            return CreateState(_cancellationTokenSource.IsCancellationRequested
                ? WritingState.Canceled
                : WritingState.Success);
        }
        catch (Exception ex)
        {
            return CreateState(WritingState.Error, ex);
        }
    }

    /// <summary>
    /// Создает состояние конвейера
    /// </summary>
    private CompositePipelineState CreateState(WritingState state, Exception? exception = null)
    {
        return new CompositePipelineState
        {
            State = state,
            Exception = exception,
            PipelineStates = _pipelines.ToDictionary(x => x.Key, x => x.State)
        };
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