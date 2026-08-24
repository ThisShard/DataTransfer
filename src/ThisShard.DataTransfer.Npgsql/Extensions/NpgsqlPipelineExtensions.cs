using ThisShard.Database.Core.Pipelines.Builders;
using ThisShard.Database.Infrastructure.Postgres.Pipelines.Builders;

namespace ThisShard.Database.Infrastructure.Extensions;

/// <summary>
/// Расширения для пайплайна Npgsql
/// </summary>
public static class NpgsqlPipelineExtensions
{
    /// <summary>
    /// Добавляет источник Npgsql
    /// </summary>
    public static IPipelineBuilder AddNpgsqlSource(this IPipelineBuilder builder,
        Action<PgPipelineSourceBuilder> configure)
    {
        var sourceBuilder = new PgPipelineSourceBuilder();
        configure(sourceBuilder);
        return builder.AddSource(sourceBuilder);
    }
    
    /// <summary>
    /// Добавляет назначение Npgsql
    /// </summary>
    public static IPipelineBuilder AddNpgsqlDestination(this IPipelineBuilder builder,
        Action<PgPipelineDestinationBuilder> configure)
    {
        var destinationBuilder = new PgPipelineDestinationBuilder();
        configure(destinationBuilder);
        return builder.AddDestination(destinationBuilder);
    }
}