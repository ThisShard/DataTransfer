
namespace ThisShard.Database.Writers.CommandFilters;

/// <summary>
/// Контекст для фабрики фильтров команд
/// </summary>
public record CommandFilterFactoryContext
{
    /// <summary>
    /// Возвращает путь к колонке источника
    /// </summary>
    public required Func<string, string> GetSourceColumnPath { get; init; }
    
    /// <summary>
    /// Возвращает путь к колонке назначения
    /// </summary>
    public required Func<string, string> GetTargetColumnPath { get; init; }
}