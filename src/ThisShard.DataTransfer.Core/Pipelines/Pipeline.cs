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
        
        var writers = await GetWriters();
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

            if (CurrentState.State != WritingState.Error)
                await CompleteWriters(writers);
        }
        finally
        {
            await DisposeHelper.DisposeMany(writers);
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
    private async ValueTask<IRowReader> GetReader(IPipelineSource source)
    {
        if (PreviousState == null || PreviousState.LastWrittenSourceKey != source.Key)
            return await source.GetReader();
        
        return await source.GetReader(PreviousState.LastWrittenRow);
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
}