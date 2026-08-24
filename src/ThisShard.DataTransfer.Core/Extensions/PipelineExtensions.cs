using ThisShard.Database.Core.Pipelines.Builders;

namespace ThisShard.Database.Core.Extensions;

/// <summary>
/// Расширения для пайплайнов
/// </summary>
public static class PipelineExtensions
{
    /// <summary>
    /// Добавляет пайплайн
    /// </summary>
    public static ICompositePipelineBuilder AddPipeline(this ICompositePipelineBuilder builder,
        Action<IPipelineBuilder> configure)
    {
        var pipelineBuilder = new PipelineBuilder();
        configure(pipelineBuilder);
        return builder.AddPipeline(pipelineBuilder);
    }
}