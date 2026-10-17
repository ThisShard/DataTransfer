using System.Runtime.InteropServices;
using ThisShard.Database.Schema.Rows;

namespace ThisShard.Database.Pipeline.Writers;

public class CompositeWriter : BufferedWriter
{
    private readonly CompositeWriterDescriptor[] _descriptors;
    
    public CompositeWriter(IEnumerable<CompositeWriterDescriptor> descriptors, int bufferSize) : base(bufferSize)
    {
        ArgumentNullException.ThrowIfNull(descriptors);

        _descriptors = descriptors.ToArray();
    }

    /// <summary>
    /// Действие при инициализации
    /// </summary>
    protected override async ValueTask OnInit()
    {
        foreach (var descriptor in _descriptors)
        {
            if (descriptor.Writer.Status == WriterStatus.Created) 
                await descriptor.Writer.Init();
        }
    }

    /// <summary>
    /// Действие при принудительной записи
    /// </summary>
    protected override async ValueTask OnFlush()
    {
        if (Buffer.Count == 0)
            return;

        var rowsToWrite = 0;
        IWriter? writer = null;

        for (var i = 0; i < Buffer.Count; i++)
        {
            //Получаем райтера и следующую пачку строк которую в него нужно записать
            if (writer == null)
            {
                var remaining = Buffer.Count - i;
                writer = GetWriterToFlush(remaining);
                rowsToWrite = GetRowsToWrite(writer, remaining);
            }

            var row = Buffer[i];
            await writer.Write(row);
            rowsToWrite--;

            //Если пачка строк закончилась, тогда флушим райтера и зануляем его для получения следующего
            if (rowsToWrite <= 0)
            {
                await writer.Flush();
                writer = null;
            }
        }

        //Флушим оставшегося райтера (по идее сюда не должно заходить)
        if (writer != null)
            await writer.Flush();
        
        ClearBuffer();
    }

    /// <summary>
    /// Возвращает количество строк для записи в текущий райтер
    /// </summary>
    private int GetRowsToWrite(IWriter writer, int remaining)
    {
        //Последний этап - райтер без буферизации - возвращаем оставшееся количество строк
        if (writer.BufferSize == 1) 
            return remaining;
        
        //Вычисляем максимальное количество пачек, которые мы можем получить из оставшихся строк
        var batches = remaining / writer.BufferSize;
        
        //Как минимум одну пачку мы должны будем записать
        if (batches <= 0)
            batches = 1;
        
        //Вычисляем количество строк для записи
        var rowsToWrite = batches * writer.BufferSize;
        
        //Если их больше оставшихся - возвращаем оставшиеся
        return rowsToWrite > remaining 
            ? remaining 
            : rowsToWrite;
    }

    /// <summary>
    /// Возвращает подходящий писатель для произведения записи
    /// </summary>
    private IWriter GetWriterToFlush(int count)
    {
        var descriptor = _descriptors
            .Where(x => x.MinRows == null || x.MinRows <= count)
            .FirstOrDefault(x => x.MaxRows == null || x.MaxRows >= count);
        
        if (descriptor == null)
            throw new InvalidOperationException($"Descriptor not found for {count} rows");

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