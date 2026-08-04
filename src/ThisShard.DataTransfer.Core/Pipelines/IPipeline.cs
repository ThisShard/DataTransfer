using ThisShard.Database.Core.Models.Results;

namespace ThisShard.Database.Core.Pipelines;

/// <summary>
/// Конвейер
/// </summary>
public interface IPipeline : IAsyncDisposable
{
    /// <summary>
    /// Ключ
    /// </summary>
    string Key { get; }
    
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
/// Конвейер с состоянием
/// </summary>
public interface IPipeline<TState> : IPipeline
{
    /// <summary>
    /// Инициализировать предыдущим состоянием перед началом работы
    /// </summary>
    void Init(TState? state);
    
    /// <summary>
    /// Состояние выполнения задач
    /// </summary>
    TState? State { get; }
}