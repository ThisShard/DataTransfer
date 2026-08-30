using ThisShard.Database.Core.Pipelines.Builders;
using ThisShard.Database.Infrastructure.Excel.Pipelines.Builders;

namespace ThisShard.Database.Infrastructure.Extensions;

/// <summary>
/// Расширения для пайплайна Excel
/// </summary>
public static class ExcelPipelineExtensions
{
    /// <summary>
    /// Добавляет источник Excel
    /// </summary>
    public static IPipelineBuilder AddExcelSource(this IPipelineBuilder builder,
        Action<ExcelPipelineSourceBuilder> configure)
    {
        var sourceBuilder = new ExcelPipelineSourceBuilder();
        configure(sourceBuilder);
        return builder.AddSource(sourceBuilder);
    }
    
    /// <summary>
    /// Добавляет назначение Excel
    /// </summary>
    public static IPipelineBuilder AddExcelDestination(this IPipelineBuilder builder,
        Action<ExcelPipelineDestinationBuilder> configure)
    {
        var destinationBuilder = new ExcelPipelineDestinationBuilder();
        configure(destinationBuilder);
        return builder.AddDestination(destinationBuilder);
    }
}