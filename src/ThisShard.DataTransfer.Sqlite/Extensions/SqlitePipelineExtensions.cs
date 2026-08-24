using ThisShard.Database.Core.Pipelines.Builders;
using ThisShard.Database.Infrastructure.Sqlite.Pipelines.Builders;

namespace ThisShard.Database.Infrastructure.Extensions;

/// <summary>
/// Расширения для пайплайна Sqlite
/// </summary>
public static class SqlitePipelineExtensions
{
    /// <summary>
    /// Добавляет источник Sqlite
    /// </summary>
    public static IPipelineBuilder AddSqliteSource(this IPipelineBuilder builder,
        Action<SqlitePipelineSourceBuilder> configure)
    {
        var sourceBuilder = new SqlitePipelineSourceBuilder();
        configure(sourceBuilder);
        return builder.AddSource(sourceBuilder);
    }
    
    /// <summary>
    /// Добавляет назначение Sqlite
    /// </summary>
    public static IPipelineBuilder AddSqliteDestination(this IPipelineBuilder builder,
        Action<SqlitePipelineDestinationBuilder> configure)
    {
        var destinationBuilder = new SqlitePipelineDestinationBuilder();
        configure(destinationBuilder);
        return builder.AddDestination(destinationBuilder);
    }
}