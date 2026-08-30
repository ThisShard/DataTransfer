namespace ThisShard.Database.Core.Helpers;

/// <summary>
/// Хелпер для диспоза
/// </summary>
public static class DisposeHelper
{
    
    /// <summary>
    /// Диспозит много объектов
    /// </summary>
    public static async ValueTask DisposeMany(Exception ex, params IAsyncDisposable[] disposables)
    {
        await DisposeMany(ex, (IEnumerable<IAsyncDisposable>)disposables);
    }
    
    /// <summary>
    /// Диспозит много объектов
    /// </summary>
    public static async ValueTask DisposeMany(Exception ex, params IEnumerable<IAsyncDisposable>[] disposables)
    {
        await DisposeMany(ex, disposables.SelectMany(x => x));
    }
    
    /// <summary>
    /// Диспозит много объектов
    /// </summary>
    public static async ValueTask DisposeMany(Exception ex, IEnumerable<IAsyncDisposable> disposables)
    {
        await DisposeManyInternal(disposables, ex);
    }
    
    /// <summary>
    /// Диспозит много объектов
    /// </summary>
    public static async ValueTask DisposeMany(params IAsyncDisposable[] disposables)
    {
        await DisposeMany((IEnumerable<IAsyncDisposable>)disposables);
    }
    
    /// <summary>
    /// Диспозит много объектов
    /// </summary>
    public static async ValueTask DisposeMany(params IEnumerable<IAsyncDisposable>[] disposables)
    {
        await DisposeMany(disposables.SelectMany(x => x));
    }
    
    /// <summary>
    /// Диспозит много объектов
    /// </summary>
    public static async ValueTask DisposeMany(IEnumerable<IAsyncDisposable> disposables)
    {
        await DisposeManyInternal(disposables, null);
    }
    
    /// <summary>
    /// Диспозит много объектов и выбрасывает исключение после этого
    /// </summary>
    private static async ValueTask DisposeManyInternal(IEnumerable<IAsyncDisposable> disposables, Exception? originalException)
    {
        var exceptions = new List<Exception>();
        
        if (originalException != null)
            exceptions.Add(originalException);
        
        foreach (var disposable in disposables)
        {
            try
            {
                await disposable.DisposeAsync();
            }
            catch (Exception ex)
            {
                exceptions.Add(ex);
            }
        }

        if (!exceptions.Any())
            return;
        
        if (exceptions.Count == 1)
            throw exceptions.Single();
        
        throw new AggregateException(exceptions);
    }
}