namespace ThisShard.Database.Extensions;

public static class CollectionExtensions
{
    public static IEnumerable<T> GetSliceEnumerable<T>(this IReadOnlyList<T> list, int start, int count)
    {
        var end = start + count;
        if (end >= list.Count)
            end = list.Count - 1;
        
        for(var i = start; i < end; i++)
        {
            yield return list[i];
        }
    }
}