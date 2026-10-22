using ThisShard.Database.Extensions;
using ThisShard.Database.Helpers;
using ThisShard.Database.Pipelines.Destinations;
using ThisShard.Database.Pipelines.Sources;
using ThisShard.Database.Pipelines.States;
using ThisShard.Database.Readers;
using ThisShard.Database.Writers;

namespace ThisShard.Database.Pipelines;

/// <summary>
/// Конвейер
/// </summary>
public class Pipeline : BasePipeline<PipelineState>
{
    private readonly IPipelineSource[] _sources;
    private readonly IPipelineDestination[] _destinations;
    
    public Pipeline(string key, IEnumerable<IPipelineSource> sources, IEnumerable<IPipelineDestination> destinations)
        : base(key)
    {
        _sources = sources?.ToArray() ?? throw new ArgumentNullException(nameof(sources));
        _destinations = destinations?.ToArray() ?? throw new ArgumentNullException(nameof(destinations));
    }

    /// <summary>
    /// Действие при выполнении
    /// </summary>
    protected override async ValueTask OnProcess(CancellationToken ct)
    {
        CurrentState = new PipelineState { State = WritingState.Success };
        
        if (ct.IsCancellationRequested)
        {
            CurrentState = CurrentState with { State = WritingState.Canceled };
            return;
        }

        List<IWriter>? writers = null;
        try
        {
            if (ct.IsCancellationRequested)
            {
                CurrentState = CurrentState with { State = WritingState.Canceled };
                return;
            }

            foreach (var source in GetSources())
            {
                if (ct.IsCancellationRequested)
                {
                    CurrentState = CurrentState with { State = WritingState.Canceled };
                    break;
                }

                await using var _ = source;
                
                writers ??= await GetWriters(source);
                
                await using var reader = await GetReader(source);
                var writingResult = await reader.TryWriteTo(writers, ct);

                CurrentState = new PipelineState
                {
                    LastWrittenSourceKey = source.Key, 
                    State = writingResult.State, 
                    LastWrittenRow = writingResult.LastWrittenRow
                };

                if (CurrentState.State != WritingState.Success)
                    break;
            }

            if (CurrentState.State != WritingState.Error && writers != null)
                await CompleteWriters(writers);
        }
        finally
        {
            if (writers != null)
                await DisposeHelper.DisposeMany(writers);
            await OnDispose();
        }
    }

    /// <summary>
    /// Действие при ошибке
    /// </summary>
    protected override ValueTask OnError(Exception ex)
    {
        CurrentState = CurrentState! with
        {
            State = WritingState.Error, 
            Exception = ex
        };
        return ValueTask.CompletedTask;
    }

    /// <summary>
    /// Действие при диспозе
    /// </summary>
    protected override async ValueTask OnDispose()
    {
        await DisposeHelper.DisposeMany(_sources, _destinations);
    }

    /// <summary>
    /// Возвращает источники с учетом предыдущего состояния
    /// </summary>
    private IEnumerable<IPipelineSource> GetSources()
    {
        if (PreviousState == null || PreviousState.LastWrittenSourceKey == null)
            return _sources;

        return _sources.SkipWhile(x => x.Key != PreviousState.LastWrittenSourceKey);
    }

    /// <summary>
    /// Возвращает ридер с учетом предыдущего состояния
    /// </summary>
    private async ValueTask<IReader> GetReader(IPipelineSource source)
    {
        if (PreviousState == null || PreviousState.LastWrittenSourceKey != source.Key)
            return await source.GetReader();
        
        return await source.GetReader(PreviousState.LastWrittenRow);
    }
    
    /// <summary>
    /// Возвращает писателей
    /// </summary>
    private async ValueTask<List<IWriter>> GetWriters(IPipelineSource source)
    {
        var writers = new List<IWriter>();
        
        try
        {
            var sourceTable = await source.GetTable();
            
            foreach (var destination in _destinations)
            {
                await destination.Init(sourceTable);
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
    private async ValueTask CompleteWriters(IEnumerable<IWriter> writers)
    {
        foreach (var writer in writers)
        {
            await writer.Complete();
        }
    }
}