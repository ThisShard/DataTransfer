using System.Data.Common;
using ThisShard.Database.Core.Models;
using ThisShard.Database.Core.Models.Rows;

namespace ThisShard.Database.Core.Readers;

/// <summary>
/// Читатель строк для DbDataReader
/// </summary>
public class DbRowReader : IRowReader, IAsyncDisposable, IDisposable
{
    private readonly DbDataReader _reader;
    private readonly bool _ownsReader;
    private readonly RowState _rowState;

    public DbRowReader(DbDataReader reader, RowState defaultRowState = RowState.Ignored, bool ownsReader = true)
    {
        _reader = reader ?? throw new ArgumentNullException(nameof(reader));
        _rowState = defaultRowState;
        _ownsReader = ownsReader;
    }

    /// <summary>
    /// Читает следующую строку
    /// </summary>
    public async ValueTask<IRow?> Read()
    {
        if (!await _reader.ReadAsync())
            return null;
        
        var data = new Dictionary<string, object?>();
        for (var i = 0; i < _reader.FieldCount; i++)
        {
            data[_reader.GetName(i)] = _reader.GetValue(i);
        }
        
        return new Row
        {
            State = _rowState,
            Data = data
        };
    }

    /// <summary>Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources asynchronously.</summary>
    /// <returns>A task that represents the asynchronous dispose operation.</returns>
    public async ValueTask DisposeAsync()
    {
        if (_ownsReader)
            await _reader.DisposeAsync();
    }

    /// <summary>Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.</summary>
    public void Dispose()
    {
        if (_ownsReader)
            _reader.Dispose();
    }
}