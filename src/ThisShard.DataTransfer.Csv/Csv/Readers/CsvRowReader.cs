using System.Text.Json;
using CsvHelper;
using ThisShard.Database.Core.Models.Rows;
using ThisShard.Database.Core.Readers;
using ThisShard.Database.Infrastructure.Csv.Helpers;

namespace ThisShard.Database.Infrastructure.Csv.Readers;

/// <summary>
/// Читатель строк Csv
/// </summary>
public class CsvRowReader : IRowReader, IDisposable
{
    private readonly CsvReader _csvReader;
    private readonly CsvParser? _csvParser;
    private readonly bool _ownsReader;
    private readonly RowState _defaultRowState;
    private readonly string? _rowStatePropertyName;
    private readonly Func<string, string>? _propertyNameResolver;
    
    private bool _isStarted;
    private bool _isFinished;

    public CsvRowReader(CsvReader csvReader, RowState defaultRowState = RowState.Ignored, Func<string, string>? propertyNameResolver = null, string? rowStatePropertyName = null, bool ownsReader = true)
    {
        _csvReader = csvReader ?? throw new ArgumentNullException(nameof(csvReader));
        _csvParser = csvReader.Parser as CsvParser;
        _ownsReader = ownsReader;
        _defaultRowState = defaultRowState;
        _propertyNameResolver = propertyNameResolver;
        _rowStatePropertyName = rowStatePropertyName;
    }

    /// <summary>
    /// Читает следующую строку
    /// </summary>
    public async ValueTask<IRow?> Read()
    {
        if (_isFinished)
            return null;

        if (!_isStarted)
        {
            if (!await _csvReader.ReadAsync())
            {
                _isFinished = true;
                return null;
            }
            
            if (_csvReader.HeaderRecord == null)
                _csvReader.ReadHeader();
            
            if (_csvReader.HeaderRecord == null)
                throw new InvalidOperationException("Header record not set");
            
            _isStarted = true;
        }

        if (!await _csvReader.ReadAsync())
        {
            _isFinished = true;
            return null;
        }

        var data = new Dictionary<string, object?>();
        var state = _defaultRowState;
        var index = 0;
        
        foreach (var column in _csvReader.HeaderRecord!)
        {
            var value = _csvReader.GetField(index);
            
            if (value == "null" && (_csvParser == null || !_csvParser.IsFieldQuoted(index)))
                value = null;
            
            if (_rowStatePropertyName == column)
                state = GetRowState(value);
            else if (_propertyNameResolver != null)
                data[_propertyNameResolver.Invoke(column)] = value;
            else
                data[column] = value;
            
            index++;
        }
        
        return new Row()
        {
            Data = data,
            State = state,
        };
    }

    /// <summary>
    /// Возвращает состояние строки из данных
    /// </summary>
    private RowState GetRowState(string? rowStateValue)
    {
        if (rowStateValue != null && Enum.TryParse(rowStateValue, true, out RowState s))
            return s;

        return _defaultRowState;
    }

    /// <summary>Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.</summary>
    public void Dispose()
    {
        if (_ownsReader)
            _csvReader.Dispose();
    }

    /// <summary>Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources asynchronously.</summary>
    /// <returns>A task that represents the asynchronous dispose operation.</returns>
    public ValueTask DisposeAsync()
    {
        Dispose();
        return ValueTask.CompletedTask;
    }
}