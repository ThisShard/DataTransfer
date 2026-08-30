using System.Collections.Concurrent;

namespace ThisShard.Database.Tests.Helpers;

public class MultiStream : IDisposable
{
    private readonly ConcurrentDictionary<string, MemoryStream> _memoryStreams = new();
    
    public MemoryStream GetStream(string key)
    {
        return _memoryStreams.GetOrAdd(key, _ => new MemoryStream());
    }

    public MemoryStream GetStreamAtBeginning(string key)
    {
        var stream = GetStream(key);
        stream.Seek(0, SeekOrigin.Begin);
        return stream;
    }
    
    public void Dispose()
    {
        foreach (var memoryStream in _memoryStreams.Values)
        {
            memoryStream.Dispose();
        }
        _memoryStreams.Clear();
    }
}