using ThisShard.Database.Core.Models.Results;

namespace ThisShard.Database.Core.Pipelines;

/// <summary>
/// Конвейер
/// </summary>
public interface IPipeline : IAsyncDisposable
{
    /// <summary>
    /// Запускает конвейер
    /// </summary>
    ValueTask Start();

    /// <summary>
    /// Останавливает конвейер
    /// </summary>
    ValueTask Stop();
    
    /// <summary>
    /// Задача завершения конвейера
    /// </summary>
    Task Completion { get; }
}

/// <summary>
/// Конвейер с результатом
/// </summary>
public interface IPipeline<out TResult> : IPipeline
{
    /// <summary>
    /// Результат завершения задачи конвейера
    /// </summary>
    TResult? Result { get; }
}

/// <summary>
/// Составной конвейер
/// </summary>
public interface ICompositePipeline<TPipeline> : IPipeline
    where TPipeline : IPipeline
{
    /// <summary>
    /// Конвейеры
    /// </summary>
    IEnumerable<TPipeline> Pipelines { get; }
}