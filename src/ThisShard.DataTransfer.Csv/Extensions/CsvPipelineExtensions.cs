using ThisShard.Database.Core.Pipelines.Builders;
using ThisShard.Database.Infrastructure.Csv.Pipelines.Builders;

namespace ThisShard.Database.Infrastructure.Extensions;

/// <summary>
/// Расширения для пайплайна Csv
/// </summary>
public static class CsvPipelineExtensions
{
    /// <summary>
    /// Добавляет источник Csv
    /// </summary>
    public static IPipelineBuilder AddCsvSource(this IPipelineBuilder builder,
        Action<CsvPipelineSourceBuilder> configure)
    {
        var sourceBuilder = new CsvPipelineSourceBuilder();
        configure(sourceBuilder);
        return builder.AddSource(sourceBuilder);
    }
    
    /// <summary>
    /// Добавляет назначение Csv
    /// </summary>
    public static IPipelineBuilder AddCsvDestination(this IPipelineBuilder builder,
        Action<CsvPipelineDestinationBuilder> configure)
    {
        var destinationBuilder = new CsvPipelineDestinationBuilder();
        configure(destinationBuilder);
        return builder.AddDestination(destinationBuilder);
    }
}