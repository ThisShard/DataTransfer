using ThisShard.Database.Core.Models;
using ThisShard.Database.Core.Models.Descriptors;
using ThisShard.Database.Core.Models.Tables;

namespace ThisShard.Database.Core.Writers;

/// <summary>
/// Составной писатель в таблицу
/// </summary>
public class CompositeTableWriter : BufferedTableWriter
{
    private readonly TableWriterDescriptor[] _descriptors;

    public CompositeTableWriter(IEnumerable<TableWriterDescriptor> descriptors, int bufferSize) : base(bufferSize)
    {
        if (descriptors == null) throw new ArgumentNullException(nameof(descriptors));
        
        _descriptors = descriptors.ToArray();
    }

    /// <summary>
    /// Действие при инициализации таблицей
    /// </summary>
    protected override async ValueTask OnInit(ITable table)
    {
        foreach (var descriptor in _descriptors)
        {
            await descriptor.Writer.Init(table);
        }
    }

    /// <summary>
    /// Действие при инициализации временной таблицей
    /// </summary>
    protected override async ValueTask OnInit(IStagingTable stagingTable)
    {
        foreach (var descriptor in _descriptors)
        {
            await descriptor.Writer.Init(stagingTable);
        }
    }

    /// <summary>
    /// Действие при принудительной записи
    /// </summary>
    protected override async ValueTask OnFlush()
    {
        if (Buffer.Count == 0)
            return;

        var writer = GetWriterToFlush();
        await writer.Write(Buffer);
        await writer.Flush();

        ClearBuffer();
    }

    /// <summary>
    /// Возвращает подходящий писатель для произведения записи
    /// </summary>
    private ITableWriter GetWriterToFlush()
    {
        var descriptor = _descriptors
            .Where(x => x.MinRows == null || x.MinRows <= Buffer.Count)
            .FirstOrDefault(x => x.MaxRows == null || x.MaxRows >= Buffer.Count);
        
        if (descriptor == null)
            throw new InvalidOperationException();

        return descriptor.Writer;
    }

    /// <summary>
    /// Действие при завершении записи
    /// </summary>
    protected override async ValueTask OnComplete()
    {
        await Flush();
        
        foreach (var descriptor in _descriptors)
        {
            await descriptor.Writer.Complete();
        }
    }

    /// <summary>
    /// Действие при очистке
    /// </summary>
    protected override async ValueTask OnDispose()
    {
        foreach (var descriptor in _descriptors.Where(x => x.Owned))
        {
            await descriptor.Writer.DisposeAsync();
        }
    }
}