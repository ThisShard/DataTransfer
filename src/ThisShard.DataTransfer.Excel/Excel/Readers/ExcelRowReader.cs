using ExcelDataReader;
using ThisShard.Database.Core.Models.Rows;
using ThisShard.Database.Core.Readers;

namespace ThisShard.Database.Infrastructure.Excel.Readers;

/// <summary>
/// Читатель строк Excel
/// </summary>
public class ExcelRowReader : IRowReader, IDisposable
{
    private readonly IExcelDataReader _excelReader;
    private readonly bool _ownsReader;
    private readonly RowState _defaultRowState;
    private readonly string? _rowStatePropertyName;
    private readonly Func<string, string>? _propertyNameResolver;
    
    private bool _isStarted;
    private bool _isFinished;

    private Dictionary<int, string> _header;

    public ExcelRowReader(IExcelDataReader excelReader, RowState defaultRowState = RowState.Ignored, Func<string, string>? propertyNameResolver = null, string? rowStatePropertyName = null, bool ownsReader = true)
    {
        _excelReader = excelReader ?? throw new ArgumentNullException(nameof(excelReader));
        _ownsReader = ownsReader;
        _defaultRowState = defaultRowState;
        _propertyNameResolver = propertyNameResolver;
        _rowStatePropertyName = rowStatePropertyName;
    }

    /// <summary>
    /// Читает следующую строку
    /// </summary>
    public ValueTask<IRow?> Read()
    {
        if (_isFinished)
            return ValueTask.FromResult<IRow?>(null);

        if (!_isStarted)
        {
            if (!_excelReader.Read())
            {
                _isFinished = true;
                return ValueTask.FromResult<IRow?>(null);
            }

            FillHeader();
            
            _isStarted = true;
        }

        if (!_excelReader.Read())
        {
            _isFinished = true;
            return ValueTask.FromResult<IRow?>(null);
        }

        var data = new Dictionary<string, object?>();
        var state = _defaultRowState;
        
        foreach (var columnMap in _header)
        {
            var value = _excelReader.GetValue(columnMap.Key);
            if (value is DateTime dateTime)
            {
                value = DateTime.SpecifyKind(dateTime, DateTimeKind.Utc);
            }
            
            if (_rowStatePropertyName == columnMap.Value)
                state = GetRowState(value?.ToString());
            else
                data[columnMap.Value] = value;
        }
        
        return ValueTask.FromResult<IRow?>(new Row()
        {
            Data = data,
            State = state,
        });
    }

    /// <summary>
    /// Заполняет заголовок
    /// </summary>
    private void FillHeader()
    {
        _header = new Dictionary<int, string>();
        for(var i = 0; i < _excelReader.FieldCount; i++)
        {
            var columnName = _excelReader.GetString(i);
            if (string.IsNullOrWhiteSpace(columnName))
                continue;
            
            _header.Add(i, _propertyNameResolver?.Invoke(columnName) ?? columnName);
        }
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
            _excelReader.Dispose();
    }

    /// <summary>Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources asynchronously.</summary>
    /// <returns>A task that represents the asynchronous dispose operation.</returns>
    public ValueTask DisposeAsync()
    {
        Dispose();
        return ValueTask.CompletedTask;
    }
}