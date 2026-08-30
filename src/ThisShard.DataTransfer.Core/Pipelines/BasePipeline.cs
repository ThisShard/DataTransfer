namespace ThisShard.Database.Core.Pipelines;

/// <summary>
/// Базовый класс конвейера
/// </summary>
public abstract class BasePipeline : IPipeline
{
    private readonly TaskCompletionSource _taskCompletionSource = new();
    private readonly CancellationTokenSource _cancellationTokenSource = new();
    
    /// <summary>
    /// Процесс уже запущен
    /// </summary>
    protected bool IsStarted { get; private set; }

    /// <summary>
    /// Ключ
    /// </summary>
    public string Key { get; }

    /// <summary>
    /// Задача завершения конвейера
    /// </summary>
    public Task Completion => _taskCompletionSource.Task;

    protected BasePipeline(string key)
    {
        Key = key;
    }
    
    /// <summary>
    /// Запускает конвейер
    /// </summary>
    public ValueTask Start()
    {
        if (IsStarted)
            return ValueTask.CompletedTask;

        IsStarted = true;

        Process(_cancellationTokenSource.Token).ContinueWith(task =>
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
    /// Останавливает конвейер
    /// </summary>
    public async ValueTask Stop()
    {
        if (!IsStarted)
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
        await OnDispose();
    }

    #region Private

    /// <summary>
    /// Останавливает работу и дожидается ее выполнения
    /// </summary>
    private async ValueTask StopAndAwaitForCompletion()
    {
        if (!IsStarted)
            return;
        
        if (_taskCompletionSource.Task.IsCompleted)
            return;

        await Stop();
        await Completion;
    }

    #endregion

    #region Protected

    /// <summary>
    /// Процесс выполнения
    /// </summary>
    protected virtual async Task Process(CancellationToken ct)
    {
        try
        {
            await OnProcess(ct);
        }
        catch (Exception ex)
        {
            await OnError(ex);
        }
    }

    #endregion
    
    #region Abstract

    /// <summary>
    /// Действие при выполнении
    /// </summary>
    protected abstract ValueTask OnProcess(CancellationToken ct);

    /// <summary>
    /// Действие при ошибке
    /// </summary>
    protected abstract ValueTask OnError(Exception ex);

    /// <summary>
    /// Действие при диспозе
    /// </summary>
    protected abstract ValueTask OnDispose();

    #endregion
}

public abstract class BasePipeline<TState> : BasePipeline, IPipeline<TState>
{
    /// <summary>
    /// Предыдущее состояние
    /// </summary>
    protected TState? PreviousState { get; private set; }
    
    /// <summary>
    /// Текущее состояние
    /// </summary>
    protected TState? CurrentState { get; set; }
    
    /// <summary>
    /// Состояние выполнения задач
    /// </summary>
    public TState? State { get; private set; }
    
    protected BasePipeline(string key) : base(key)
    {
    }

    /// <summary>
    /// Инициализировать предыдущим состоянием перед началом работы
    /// </summary>
    public void Init(TState? state)
    {
        if (IsStarted)
            throw new InvalidOperationException("Pipeline is already started");

        PreviousState = state;
    }

    /// <summary>
    /// Процесс
    /// </summary>
    protected override async Task Process(CancellationToken ct)
    {
        await base.Process(ct);
        State = CurrentState;
    }
}