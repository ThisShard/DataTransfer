namespace ThisShard.Database.Core.Models.Results;

public record CompositeWritingResult
{
    public WritingResult[] Results { get; init; }
}