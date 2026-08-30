using ThisShard.Database.Core.Pipelines.Builders;
using ThisShard.Database.Infrastructure.Json.Pipelines.Builders;

namespace ThisShard.Database.Infrastructure.Extensions;

/// <summary>
/// Расширения для пайплайна Json
/// </summary>
public static class JsonPipelineExtensions
{
    /// <summary>
    /// Добавляет источник Json
    /// </summary>
    public static IPipelineBuilder AddJsonSource(this IPipelineBuilder builder,
        Action<JsonPipelineSourceBuilder> configure)
    {
        var sourceBuilder = new JsonPipelineSourceBuilder();
        configure(sourceBuilder);
        return builder.AddSource(sourceBuilder);
    }
    
    /// <summary>
    /// Добавляет назначение Json
    /// </summary>
    public static IPipelineBuilder AddJsonDestination(this IPipelineBuilder builder,
        Action<JsonPipelineDestinationBuilder> configure)
    {
        var destinationBuilder = new JsonPipelineDestinationBuilder();
        configure(destinationBuilder);
        return builder.AddDestination(destinationBuilder);
    }
}