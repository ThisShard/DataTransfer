namespace ThisShard.Database.Core.Models.Filters;

/// <summary>
/// Дополнительное условие фильтрации для команд писателя
/// </summary>
public record CommandFilter<TParameter>
{
    /// <summary>
    /// Текст условия команды
    /// </summary>
    public required string CommandFilterText { get; init; }

    /// <summary>
    /// Дополнительные параметры которые нужно добавить к команде
    /// </summary>
    public IReadOnlyCollection<Func<TParameter>> Parameters { get; init; } = Array.Empty<Func<TParameter>>();
}