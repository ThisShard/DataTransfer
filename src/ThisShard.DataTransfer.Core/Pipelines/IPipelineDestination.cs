using ThisShard.Database.Core.Writers;

namespace ThisShard.Database.Core.Pipelines;

public interface IPipelineDestination
{
    /// <summary>
    /// Возвращает писателя
    /// </summary>
    public IRowWriter GetWriter();
}