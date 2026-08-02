namespace ThisShard.Database.Core.Pipelines;

/// <summary>
/// Конвейер
/// </summary>
public interface IPipeline
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
    ValueTask Completion { get; }
}