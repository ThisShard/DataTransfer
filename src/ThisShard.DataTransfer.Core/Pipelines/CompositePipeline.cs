using ThisShard.Database.Core.Helpers;
using ThisShard.Database.Core.Models.Results;
using ThisShard.Database.Core.Models.States;

namespace ThisShard.Database.Core.Pipelines;

/// <summary>
/// Составной конвейер
/// </summary>
public class CompositePipeline : BasePipeline<CompositePipelineState>
{
    private readonly IPipeline<PipelineState>[] _pipelines;
    private readonly int _maxDop;
    
    public CompositePipeline(string key, IEnumerable<IPipeline<PipelineState>> pipelines, int maxDop = 1)
        : base(key)
    {
        _pipelines = pipelines?.ToArray() ?? throw new ArgumentNullException(nameof(pipelines));
        _maxDop = maxDop;
    }

    /// <summary>
    /// Действие при выполнении
    /// </summary>
    protected override async ValueTask OnProcess(CancellationToken ct)
    {
        if (ct.IsCancellationRequested)
        {
            CurrentState = CreateState(WritingState.Canceled);
            return;
        }
        
        if (_maxDop > 1)
            await ProcessParallel(ct);
        else
            await ProcessSequential(ct);

        CurrentState = CreateState(ct.IsCancellationRequested
            ? WritingState.Canceled
            : WritingState.Success);
    }

    /// <summary>
    /// Действие при ошибке
    /// </summary>
    protected override ValueTask OnError(Exception ex)
    {
        CurrentState = CreateState(WritingState.Error, ex);
        return ValueTask.CompletedTask;
    }

    /// <summary>
    /// Параллельная обработка конвейеров
    /// </summary>
    private async Task ProcessParallel(CancellationToken ct)
    {
        await Parallel.ForEachAsync(_pipelines, new ParallelOptions()
        {
            CancellationToken = ct,
            MaxDegreeOfParallelism = _maxDop
        }, ProcessSingle);
    }

    /// <summary>
    /// Последовательная обработка конвейеров
    /// </summary>
    private async Task ProcessSequential(CancellationToken ct)
    {
        foreach (var pipeline in _pipelines)
        {
            await ProcessSingle(pipeline, ct);
        }
    }
    
    /// <summary>
    /// Обработка одиночного конвейера
    /// </summary>
    private async ValueTask ProcessSingle(IPipeline<PipelineState> pipeline, CancellationToken ct)
    {
        if (ct.IsCancellationRequested)
            return;
            
        var previousState = PreviousState?.PipelineStates.GetValueOrDefault(pipeline.Key);
        if (previousState != null)
        {
            if (previousState.State == WritingState.Success)
                return;
                
            pipeline.Init(previousState);
        }
            
        await pipeline.Start();
        await pipeline.Completion;
    }

    /// <summary>
    /// Действие при диспозе
    /// </summary>
    protected override async ValueTask OnDispose()
    {
        await DisposeHelper.DisposeMany(_pipelines.Cast<IAsyncDisposable>());
    }

    /// <summary>
    /// Создает состояние конвейера
    /// </summary>
    private CompositePipelineState CreateState(WritingState state, Exception? exception = null)
    {
        var aggregateException = GetAggregateException(exception);

        return new CompositePipelineState
        {
            State = _pipelines.Any(x => x.State?.State == WritingState.Error) 
                ? WritingState.Error 
                : state,
            Exception = aggregateException,
            PipelineStates = _pipelines.ToDictionary(x => x.Key, x => x.State)
        };
    }

    /// <summary>
    /// Формирует агрегированное исключение
    /// </summary>
    private Exception? GetAggregateException(Exception? exception)
    {
        var errorPipelines = _pipelines
            .Where(x => x.State?.State == WritingState.Error);
        
        var exceptions = errorPipelines
            .Select(x => x.State!.Exception)
            .Concat([exception])
            .Where(x => x != null)
            .ToList();
        
        if (exceptions.Count > 1)
            exception = new AggregateException(exceptions!);
        else
            exception = exceptions.FirstOrDefault();
        
        return exception;
    }
}